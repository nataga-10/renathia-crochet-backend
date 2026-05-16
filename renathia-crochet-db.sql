-- ============================================================
-- Renathia Crochet - E-Commerce Database
-- Script: Stored Procedures & Triggers (columnas corregidas)
-- Base de datos: Azure SQL Server
-- Fecha: 2026-05-16
-- ============================================================
-- Columnas de referencia usadas en este script:
--   Orders      : OrderId, UserId, DeliveryMethod, ShippingAddressId, ShippingRateId,
--                 ShippingCost, Subtotal, Total, Status, Notes, CreatedAt, UpdatedAt, ShippingAddress
--   OrderItems  : OrderItemId, OrderId, ProductId, ProductColorId,
--                 Quantity, UnitPrice, Subtotal, Notes
--   Products    : ProductId, CategoryId, Name, Description, BasePrice,
--                 Stock, IsMadeToOrder, IsActive, CreatedAt, UpdatedAt
--   Payments    : PaymentId, OrderId, WompiReference, WompiTransactionId,
--                 Method, Amount, Status, TransactionLog, CreatedAt, UpdatedAt
--   Users       : UserId, Name, Email, Password, RoleId
--   OrderTracking: TrackingId, OrderId, Status, Notes, CreatedAt
-- ============================================================


-- ============================================================
--  STORED PROCEDURES
-- ============================================================

-- ============================================================
-- SP 1: Reporte de ventas por período
--   Parámetros : @FechaInicio DATE, @FechaFin DATE
--   Retorna    : detalle por día + resumen global del período
--                Excluye órdenes Cancelled y Pending
-- ============================================================
CREATE OR ALTER PROCEDURE sp_ReporteVentasPorPeriodo
    @FechaInicio DATE,
    @FechaFin    DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaInicio > @FechaFin
    BEGIN
        RAISERROR('La fecha de inicio no puede ser mayor que la fecha de fin.', 16, 1);
        RETURN;
    END

    -- -- Detalle por día --
    SELECT
        CAST(o.CreatedAt AS DATE)          AS Fecha,
        COUNT(DISTINCT o.OrderId)          AS TotalOrdenes,
        SUM(oi.Quantity)                   AS TotalUnidades,
        SUM(o.Subtotal)                    AS TotalSubtotal,
        SUM(o.ShippingCost)                AS TotalEnvio,
        SUM(o.Total)                       AS TotalIngresos,
        AVG(o.Total)                       AS PromedioOrden
    FROM Orders o
    INNER JOIN OrderItems oi ON oi.OrderId = o.OrderId
    WHERE
        CAST(o.CreatedAt AS DATE) BETWEEN @FechaInicio AND @FechaFin
        AND o.Status NOT IN ('Cancelled', 'Pending')
    GROUP BY CAST(o.CreatedAt AS DATE)
    ORDER BY Fecha;

    -- -- Resumen global del período --
    SELECT
        COUNT(DISTINCT o.OrderId)          AS TotalOrdenes,
        SUM(oi.Quantity)                   AS TotalUnidadesVendidas,
        SUM(o.Subtotal)                    AS TotalSubtotal,
        SUM(o.ShippingCost)                AS TotalEnvio,
        SUM(o.Total)                       AS TotalIngresos,
        AVG(o.Total)                       AS PromedioOrden,
        MAX(o.Total)                       AS OrdenMaxima,
        MIN(o.Total)                       AS OrdenMinima
    FROM Orders o
    INNER JOIN OrderItems oi ON oi.OrderId = o.OrderId
    WHERE
        CAST(o.CreatedAt AS DATE) BETWEEN @FechaInicio AND @FechaFin
        AND o.Status NOT IN ('Cancelled', 'Pending');
END;
GO


-- ============================================================
-- SP 2: Productos más vendidos (Top N)
--   Parámetro : @TopN INT  (default 10)
--   Retorna   : ranking por unidades vendidas con ingresos generados
-- ============================================================
CREATE OR ALTER PROCEDURE sp_ProductosMasVendidos
    @TopN INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    IF @TopN <= 0
    BEGIN
        RAISERROR('El parámetro @TopN debe ser mayor que 0.', 16, 1);
        RETURN;
    END

    SELECT TOP (@TopN)
        p.ProductId                              AS ProductoId,
        p.Name                                   AS Producto,
        p.BasePrice                              AS PrecioBase,
        p.Stock                                  AS StockActual,
        p.IsActive                               AS Activo,
        SUM(oi.Quantity)                         AS TotalUnidadesVendidas,
        COUNT(DISTINCT oi.OrderId)               AS TotalOrdenes,
        SUM(oi.Quantity * oi.UnitPrice)          AS TotalIngresosGenerados
    FROM Products p
    INNER JOIN OrderItems oi ON oi.ProductId = p.ProductId
    INNER JOIN Orders     o  ON o.OrderId    = oi.OrderId
    WHERE o.Status NOT IN ('Cancelled', 'Pending')
    GROUP BY p.ProductId, p.Name, p.BasePrice, p.Stock, p.IsActive
    ORDER BY TotalUnidadesVendidas DESC;
END;
GO


-- ============================================================
-- SP 3: Historial completo de un pedido con tracking
--   Parámetro : @OrderId INT
--   Retorna   : 3 result sets:
--               1. Encabezado del pedido + cliente + pago
--               2. Ítems del pedido
--               3. Timeline de OrderTracking
-- ============================================================
CREATE OR ALTER PROCEDURE sp_HistorialPedido
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = @OrderId)
    BEGIN
        RAISERROR('El pedido con Id %d no existe.', 16, 1, @OrderId);
        RETURN;
    END

    -- -- 1. Encabezado: pedido + cliente + pago --
    SELECT
        o.OrderId,
        o.Status                AS EstadoActual,
        o.DeliveryMethod        AS MetodoEntrega,
        o.ShippingAddress       AS DireccionEnvio,
        o.ShippingCost          AS CostoEnvio,
        o.Subtotal,
        o.Total,
        o.Notes                 AS NotasPedido,
        o.CreatedAt             AS FechaPedido,
        u.UserId                AS ClienteId,
        u.FullName              AS ClienteNombre,
        u.Email                 AS ClienteEmail,
        pay.PaymentId,
        pay.Amount              AS MontoPago,
        pay.Status              AS EstadoPago,
        pay.Method              AS MetodoPago,
        pay.WompiReference      AS ReferenciaWompi,
        pay.WompiTransactionId  AS TransaccionWompi
    FROM Orders o
    INNER JOIN Users    u   ON u.UserId    = o.UserId
    LEFT  JOIN Payments pay ON pay.OrderId = o.OrderId
    WHERE o.OrderId = @OrderId;

    -- -- 2. Ítems del pedido --
    SELECT
        oi.OrderItemId,
        p.ProductId,
        p.Name              AS Producto,
        oi.Quantity         AS Cantidad,
        oi.UnitPrice        AS PrecioUnitario,
        oi.Subtotal         AS SubtotalItem,
        oi.Notes            AS NotasItem
    FROM OrderItems oi
    INNER JOIN Products p ON p.ProductId = oi.ProductId
    WHERE oi.OrderId = @OrderId
    ORDER BY oi.OrderItemId;

    -- -- 3. Historial de tracking (línea de tiempo) --
    SELECT
        ot.TrackingId   AS TrackingId,
        ot.Status       AS Estado,
        ot.Notes        AS Notas,
        ot.CreatedAt    AS FechaHora
    FROM OrderTracking ot
    WHERE ot.OrderId = @OrderId
    ORDER BY ot.CreatedAt ASC;
END;
GO


-- ============================================================
-- SP 4: Todos los pedidos de un cliente
--   Parámetro : @UserId INT
--   Retorna   : lista de órdenes con último estado de tracking
-- ============================================================
CREATE OR ALTER PROCEDURE sp_PedidosPorCliente
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = @UserId)
    BEGIN
        RAISERROR('El usuario con Id %d no existe.', 16, 1, @UserId);
        RETURN;
    END

    SELECT
        o.OrderId,
        o.Status                        AS Estado,
        o.DeliveryMethod                AS MetodoEntrega,
        o.ShippingAddress               AS DireccionEnvio,
        o.ShippingCost                  AS CostoEnvio,
        o.Subtotal,
        o.Total,
        o.Notes                         AS Notas,
        o.CreatedAt                     AS FechaPedido,
        COUNT(oi.OrderItemId)           AS TotalProductos,
        SUM(oi.Quantity)                AS TotalUnidades,
        pay.Status                      AS EstadoPago,
        pay.WompiReference              AS ReferenciaWompi,
        ot.LastTracking                 AS UltimoEstadoTracking,
        ot.LastTrackingDate             AS FechaUltimoTracking
    FROM Orders o
    LEFT JOIN OrderItems oi ON oi.OrderId = o.OrderId
    LEFT JOIN Payments  pay ON pay.OrderId = o.OrderId
    -- Último registro de tracking por orden
    LEFT JOIN (
        SELECT
            OrderId,
            Status    AS LastTracking,
            CreatedAt AS LastTrackingDate,
            ROW_NUMBER() OVER (PARTITION BY OrderId ORDER BY CreatedAt DESC) AS rn
        FROM OrderTracking
    ) ot ON ot.OrderId = o.OrderId AND ot.rn = 1
    WHERE o.UserId = @UserId
    GROUP BY
        o.OrderId, o.Status, o.DeliveryMethod, o.ShippingAddress,
        o.ShippingCost, o.Subtotal, o.Total, o.Notes, o.CreatedAt,
        pay.Status, pay.WompiReference,
        ot.LastTracking, ot.LastTrackingDate
    ORDER BY o.OrderId DESC;
END;
GO


-- ============================================================
-- SP 5: Actualizar stock de un producto después de una venta
--   Parámetros : @ProductId INT, @CantidadVendida INT
--   Uso        : ajuste manual de stock; el TR2 lo hace automáticamente
--                al insertar OrderItems. Usar este SP para correcciones.
-- ============================================================
CREATE OR ALTER PROCEDURE sp_ActualizarStockProducto
    @ProductId       INT,
    @CantidadVendida INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @CantidadVendida <= 0
    BEGIN
        RAISERROR('La cantidad vendida debe ser mayor que 0.', 16, 1);
        RETURN;
    END

    DECLARE @StockActual INT;

    SELECT @StockActual = Stock
    FROM Products
    WHERE ProductId = @ProductId;

    IF @StockActual IS NULL
    BEGIN
        RAISERROR('El producto con Id %d no existe.', 16, 1, @ProductId);
        RETURN;
    END

    IF @StockActual < @CantidadVendida
    BEGIN
        RAISERROR(
            'Stock insuficiente. Disponible: %d, solicitado: %d.',
            16, 1, @StockActual, @CantidadVendida
        );
        RETURN;
    END

    UPDATE Products
    SET
        Stock    = Stock - @CantidadVendida,
        -- Si el nuevo stock queda en 0 se inactiva (TR4 también lo cubre vía UPDATE)
        IsActive = CASE WHEN (Stock - @CantidadVendida) = 0 THEN 0 ELSE IsActive END,
        UpdatedAt = GETUTCDATE()
    WHERE ProductId = @ProductId;

    -- Confirmación: estado actual del producto
    SELECT
        ProductId,
        Name        AS Producto,
        Stock       AS StockActualizado,
        IsActive    AS Activo
    FROM Products
    WHERE ProductId = @ProductId;
END;
GO


-- ============================================================
--  TRIGGERS
-- ============================================================

-- ============================================================
-- TR 1: Insertar en OrderTracking al cambiar Status de una Order
--   Tabla  : Orders
--   Evento : AFTER UPDATE
--   Acción : registra la transición de estado con timestamp UTC
-- ============================================================
CREATE OR ALTER TRIGGER tr_RegistrarCambioEstadoPedido
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Status)
    BEGIN
        INSERT INTO OrderTracking (OrderId, Status, Notes, CreatedAt)
        SELECT
            i.OrderId,
            i.Status,
            CONCAT('Estado actualizado de [', d.Status, '] a [', i.Status, ']'),
            GETUTCDATE()
        FROM inserted i
        INNER JOIN deleted d ON d.OrderId = i.OrderId
        WHERE i.Status <> d.Status; -- Solo si hubo un cambio real
    END
END;
GO


-- ============================================================
-- TR 2: Descontar stock al insertar un OrderItem
--   Tabla  : OrderItems
--   Evento : AFTER INSERT
--   Acción : valida stock suficiente y descuenta; ROLLBACK si no alcanza
-- ============================================================
CREATE OR ALTER TRIGGER tr_DescontarStockAlInsertar
ON OrderItems
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Verificar stock suficiente para TODOS los ítems insertados
    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN Products p ON p.ProductId = i.ProductId
        WHERE p.Stock < i.Quantity
    )
    BEGIN
        RAISERROR('Stock insuficiente para uno o más productos del pedido.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- Descontar stock en una sola operación SET-based
    UPDATE p
    SET
        p.Stock     = p.Stock - i.Quantity,
        p.UpdatedAt = GETUTCDATE()
    FROM Products p
    INNER JOIN inserted i ON i.ProductId = p.ProductId;
END;
GO


-- ============================================================
-- TR 3: Recalcular Subtotal y Total de la Order al modificar OrderItems
--   Tabla  : OrderItems
--   Evento : AFTER INSERT, UPDATE, DELETE
--   Acción : recalcula Orders.Subtotal y Orders.Total (Subtotal + ShippingCost)
-- ============================================================
CREATE OR ALTER TRIGGER tr_ActualizarTotalOrden
ON OrderItems
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Reunir todos los OrderId afectados por la operación
    DECLARE @OrdenesAfectadas TABLE (OrderId INT PRIMARY KEY);

    INSERT INTO @OrdenesAfectadas (OrderId)
    SELECT DISTINCT OrderId FROM inserted
    UNION
    SELECT DISTINCT OrderId FROM deleted;

    -- Recalcular Subtotal = SUM(Quantity * UnitPrice) por orden
    -- Recalcular Total    = Subtotal + ShippingCost
    UPDATE o
    SET
        o.Subtotal  = ISNULL(calc.NuevoSubtotal, 0),
        o.Total     = ISNULL(calc.NuevoSubtotal, 0) + ISNULL(o.ShippingCost, 0),
        o.UpdatedAt = GETUTCDATE()
    FROM Orders o
    INNER JOIN @OrdenesAfectadas oa ON oa.OrderId = o.OrderId
    LEFT JOIN (
        SELECT
            OrderId,
            SUM(Quantity * UnitPrice) AS NuevoSubtotal
        FROM OrderItems
        WHERE OrderId IN (SELECT OrderId FROM @OrdenesAfectadas)
        GROUP BY OrderId
    ) calc ON calc.OrderId = o.OrderId;
END;
GO


-- ============================================================
-- TR 4: Marcar producto como inactivo cuando su Stock llega a 0
--   Tabla  : Products
--   Evento : AFTER UPDATE
--   Acción : si Stock = 0 y el producto estaba activo, lo inactiva
-- ============================================================
CREATE OR ALTER TRIGGER tr_InactivarProductoSinStock
ON Products
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Stock)
    BEGIN
        UPDATE p
        SET
            p.IsActive  = 0,
            p.UpdatedAt = GETUTCDATE()
        FROM Products p
        INNER JOIN inserted i ON i.ProductId = p.ProductId
        WHERE i.Stock = 0
          AND i.IsActive = 1; -- Evitar UPDATE innecesario si ya estaba inactivo
    END
END;
GO


-- ============================================================
-- TR 5: Crear registro en Payments al pasar Order a 'PaymentReceived'
--   Tabla  : Orders
--   Evento : AFTER UPDATE
--   Acción : inserta en Payments solo en la transición hacia PaymentReceived
--            y solo si aún no existe un pago para esa orden
-- ============================================================
CREATE OR ALTER TRIGGER tr_RegistrarPagoAlConfirmar
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(Status)
    BEGIN
        INSERT INTO Payments
            (OrderId, WompiReference, WompiTransactionId, Method, Amount, Status, TransactionLog, CreatedAt, UpdatedAt)
        SELECT
            i.OrderId,
            -- Referencia provisional; será reemplazada por el webhook de Wompi
            CONCAT('AUTO-PAY-', i.OrderId, '-', FORMAT(GETUTCDATE(), 'yyyyMMddHHmmss')),
            NULL,                   -- WompiTransactionId: se actualiza vía webhook
            'Pending',              -- Method: se actualiza cuando llega la confirmación
            i.Total,
            'Confirmed',
            NULL,                   -- TransactionLog: se llena con el payload del webhook
            GETUTCDATE(),
            GETUTCDATE()
        FROM inserted i
        INNER JOIN deleted d ON d.OrderId = i.OrderId
        WHERE i.Status  = 'PaymentReceived'
          AND d.Status <> 'PaymentReceived'   -- Solo en la transición, no en re-saves
          AND NOT EXISTS (                    -- Evitar duplicados
              SELECT 1 FROM Payments px WHERE px.OrderId = i.OrderId
          );
    END
END;
GO


-- ============================================================
-- FIN DEL SCRIPT
-- ============================================================
