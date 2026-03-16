-- ============================================================
--  RENATHIA CROCHET - Script de Base de Datos
--  Motor: SQL Server | Proyecto: E-commerce Artesanal
--  Desarrollado por: Nathalia Gutiérrez Alarcón
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'RenathiaCrochet')
    CREATE DATABASE RenathiaCrochet;
GO

USE RenathiaCrochet;
GO

-- ============================================================
-- 1. ROLES
-- ============================================================
CREATE TABLE Roles (
    RoleId   INT            IDENTITY(1,1) PRIMARY KEY,
    Name     NVARCHAR(50)   NOT NULL UNIQUE  -- 'Admin', 'Client'
);

-- ============================================================
-- 2. USUARIOS
-- ============================================================
CREATE TABLE Users (
    UserId        INT            IDENTITY(1,1) PRIMARY KEY,
    RoleId        INT            NOT NULL REFERENCES Roles(RoleId),
    FullName      NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(150)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(256)  NOT NULL,       -- BCrypt
    Phone         NVARCHAR(20),
    IsActive      BIT            NOT NULL DEFAULT 1,
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt     DATETIME2
);

-- ============================================================
-- 3. DIRECCIONES DEL CLIENTE
--    Múltiples direcciones por usuario, con opción de default
-- ============================================================
CREATE TABLE UserAddresses (
    AddressId    INT           IDENTITY(1,1) PRIMARY KEY,
    UserId       INT           NOT NULL REFERENCES Users(UserId),
    RecipientName NVARCHAR(100) NOT NULL,
    Department   NVARCHAR(100) NOT NULL,
    City         NVARCHAR(100) NOT NULL,
    Address      NVARCHAR(255) NOT NULL,
    PostalCode   NVARCHAR(10),
    IsDefault    BIT           NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- 4. TARIFAS DE ENVÍO POR DEPARTAMENTO (Colombia)
--    El flete se calcula con base en el departamento del cliente
-- ============================================================
CREATE TABLE ShippingRates (
    ShippingRateId  INT           IDENTITY(1,1) PRIMARY KEY,
    Department      NVARCHAR(100) NOT NULL UNIQUE,
    Price           DECIMAL(10,2) NOT NULL,        -- en COP
    EstimatedDays   INT           NOT NULL,
    IsActive        BIT           NOT NULL DEFAULT 1
);

-- ============================================================
-- 5. CATEGORÍAS DE PRODUCTO
-- ============================================================
CREATE TABLE Categories (
    CategoryId   INT           IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL,
    Description  NVARCHAR(255),
    IsActive     BIT           NOT NULL DEFAULT 1
);

-- ============================================================
-- 6. PRODUCTOS
--    Stock numérico + bandera IsMadeToOrder:
--    si Stock = 0 e IsMadeToOrder = 1 → se elabora bajo pedido
-- ============================================================
CREATE TABLE Products (
    ProductId      INT            IDENTITY(1,1) PRIMARY KEY,
    CategoryId     INT            REFERENCES Categories(CategoryId),
    Name           NVARCHAR(150)  NOT NULL,
    Description    NVARCHAR(MAX),
    BasePrice      DECIMAL(10,2)  NOT NULL,
    Stock          INT            NOT NULL DEFAULT 0,
    IsMadeToOrder  BIT            NOT NULL DEFAULT 0,
    IsActive       BIT            NOT NULL DEFAULT 1,
    CreatedAt      DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt      DATETIME2
);

-- ============================================================
-- 7. COLORES DISPONIBLES POR PRODUCTO
-- ============================================================
CREATE TABLE ProductColors (
    ProductColorId  INT          IDENTITY(1,1) PRIMARY KEY,
    ProductId       INT          NOT NULL REFERENCES Products(ProductId),
    ColorName       NVARCHAR(50) NOT NULL,
    ColorHex        NVARCHAR(7),                   -- ej. #FF5733
    IsAvailable     BIT          NOT NULL DEFAULT 1
);

-- ============================================================
-- 8. IMÁGENES DEL PRODUCTO 
-- ============================================================
CREATE TABLE ProductImages (
    ProductImageId  INT           IDENTITY(1,1) PRIMARY KEY,
    ProductId       INT           NOT NULL REFERENCES Products(ProductId),
    ImageUrl        NVARCHAR(500) NOT NULL,
    IsPrimary       BIT           NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- 9. PEDIDOS
--    DeliveryMethod: 'Shipping' | 'Pickup'
--    Si es Pickup → ShippingCost = 0 y no se requiere dirección
-- ============================================================
CREATE TABLE Orders (
    OrderId            INT            IDENTITY(1,1) PRIMARY KEY,
    UserId             INT            NOT NULL REFERENCES Users(UserId),

    -- Método de entrega
    DeliveryMethod     NVARCHAR(10)   NOT NULL CHECK (DeliveryMethod IN ('Shipping', 'Pickup')),

    -- Datos de envío (NULL si es Pickup)
    ShippingAddressId  INT            REFERENCES UserAddresses(AddressId),
    ShippingRateId     INT            REFERENCES ShippingRates(ShippingRateId),
    ShippingCost       DECIMAL(10,2)  NOT NULL DEFAULT 0,

    -- Totales calculados al momento del pedido
    Subtotal           DECIMAL(10,2)  NOT NULL,
    Total              DECIMAL(10,2)  NOT NULL,    -- Subtotal + ShippingCost

    -- Estado general del pedido (sincronizado con el último tracking)
    Status             NVARCHAR(30)   NOT NULL DEFAULT 'PendingPayment',

    Notes              NVARCHAR(500),               -- notas opcionales del cliente
    CreatedAt          DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt          DATETIME2
);

-- ============================================================
-- 10. DETALLE DE ITEMS DEL PEDIDO
--     Guarda precio unitario al momento de la compra
-- ============================================================
CREATE TABLE OrderItems (
    OrderItemId     INT           IDENTITY(1,1) PRIMARY KEY,
    OrderId         INT           NOT NULL REFERENCES Orders(OrderId),
    ProductId       INT           NOT NULL REFERENCES Products(ProductId),
    ProductColorId  INT           REFERENCES ProductColors(ProductColorId),  -- color elegido
    Quantity        INT           NOT NULL DEFAULT 1,
    UnitPrice       DECIMAL(10,2) NOT NULL,          -- precio capturado en el momento
    Subtotal        AS (Quantity * UnitPrice) PERSISTED
);

-- ============================================================
-- 11. TRACKER DE ESTADOS DEL PEDIDO
--
--   Estados para ENVÍO (Shipping):
--     PendingPayment → PaymentReceived → InProduction
--     → QualityControl → Shipped → Delivered
--
--   Estados para RECOGIDA (Pickup):
--     PendingPayment → PaymentReceived → InProduction
--     → QualityControl → ReadyForPickup → PickedUp
--
--   Cada cambio de estado genera un nuevo registro (auditoría completa)
-- ============================================================
CREATE TABLE OrderTracking (
    TrackingId   INT            IDENTITY(1,1) PRIMARY KEY,
    OrderId      INT            NOT NULL REFERENCES Orders(OrderId),
    Status       NVARCHAR(50)   NOT NULL,
    Notes        NVARCHAR(255),
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE(),
    CreatedBy    INT            REFERENCES Users(UserId)   -- admin que actualizó
);

-- ============================================================
-- 12. PAGOS (Wompi)
-- ============================================================
CREATE TABLE Payments (
    PaymentId           INT            IDENTITY(1,1) PRIMARY KEY,
    OrderId             INT            NOT NULL REFERENCES Orders(OrderId),
    WompiReference      NVARCHAR(100)  NOT NULL UNIQUE,  -- referencia enviada a Wompi
    WompiTransactionId  NVARCHAR(100),                   -- ID que devuelve Wompi
    Method              NVARCHAR(20)   NOT NULL,         -- 'PSE', 'Card', 'Nequi'
    Amount              DECIMAL(10,2)  NOT NULL,
    -- PENDING | APPROVED | DECLINED | VOIDED | ERROR
    Status              NVARCHAR(20)   NOT NULL DEFAULT 'PENDING',
    TransactionLog      NVARCHAR(MAX),                   -- JSON del webhook completo
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETDATE(),
    UpdatedAt           DATETIME2
);

-- ============================================================
-- 13. GALERÍA COMUNITARIA
--     Cualquier cliente puede subir fotos (libre, no vinculada a pedido)
--     El admin aprueba antes de publicar
-- ============================================================
CREATE TABLE Gallery (
    GalleryId    INT           IDENTITY(1,1) PRIMARY KEY,
    UserId       INT           NOT NULL REFERENCES Users(UserId),
    ImageUrl     NVARCHAR(500) NOT NULL,     -- Azure Blob Storage
    Caption      NVARCHAR(255),
    IsApproved   BIT           NOT NULL DEFAULT 0,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE(),
    ApprovedAt   DATETIME2,
    ApprovedBy   INT           REFERENCES Users(UserId)
);


-- ============================================================
-- ÍNDICES DE RENDIMIENTO
-- ============================================================
CREATE INDEX IX_Users_Email          ON Users(Email);
CREATE INDEX IX_Orders_UserId        ON Orders(UserId);
CREATE INDEX IX_Orders_Status        ON Orders(Status);
CREATE INDEX IX_OrderItems_OrderId   ON OrderItems(OrderId);
CREATE INDEX IX_OrderTracking_Order  ON OrderTracking(OrderId);
CREATE INDEX IX_Payments_OrderId     ON Payments(OrderId);
CREATE INDEX IX_Gallery_UserId       ON Gallery(UserId);
CREATE INDEX IX_Gallery_IsApproved   ON Gallery(IsApproved);
CREATE INDEX IX_ProductColors_Prod   ON ProductColors(ProductId);
CREATE INDEX IX_ProductImages_Prod   ON ProductImages(ProductId);
GO

