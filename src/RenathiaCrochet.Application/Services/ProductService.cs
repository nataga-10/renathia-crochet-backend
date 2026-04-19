using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Servicio de lógica de negocio para la gestión del catálogo de productos.
    /// Coordina el repositorio de productos con el almacenamiento de imágenes en Azure.
    /// </summary>
    public class ProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IBlobStorageService _blobStorageService;

        public ProductService(IProductRepository productRepository, IBlobStorageService blobStorageService)
        {
            _productRepository = productRepository;
            _blobStorageService = blobStorageService;
        }

        /// <summary>Retorna todos los productos activos mapeados a DTOs para el catálogo.</summary>
        public async Task<List<ProductDto>> GetAllActiveAsync()
        {
            var products = await _productRepository.GetAllActiveAsync();
            return MapToDto(products);
        }

        /// <summary>Retorna los productos activos de una categoría específica.</summary>
        public async Task<List<ProductDto>> GetByCategoryAsync(int categoryId)
        {
            var products = await _productRepository.GetByCategoryAsync(categoryId);
            return MapToDto(products);
        }

        public async Task<ProductDto?> GetByIdAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return null;
            return MapToDto(new List<Product> { product }).First();
        }

        /// <summary>
        /// Crea un producto nuevo. Si se proporciona una imagen, la sube a Azure Blob Storage
        /// y la asocia como imagen primaria. El nombre del blob incluye el ID del producto
        /// para evitar colisiones de nombres.
        /// </summary>
        public async Task<ProductDto> CreateAsync(CreateProductDto dto, Stream? imageStream, string? fileName)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                BasePrice = dto.BasePrice,
                Stock = dto.Stock,
                IsMadeToOrder = dto.IsMadeToOrder,
                CategoryId = dto.CategoryId,
                IsActive = true,
                // Crear variantes de color a partir de la lista de nombres
                Colors = dto.Colors.Select(c => new ProductColor { ColorName = c }).ToList()
            };

            await _productRepository.AddAsync(product);

            // Subir imagen solo si se proporcionó una
            if (imageStream != null && fileName != null)
            {
                // Prefijo con ProductId para evitar sobreescribir imágenes de otros productos
                var imageUrl = await _blobStorageService.UploadImageAsync(imageStream, $"{product.ProductId}-{fileName}");
                product.Images.Add(new ProductImage { ImageUrl = imageUrl, IsPrimary = true, ProductId = product.ProductId });
                await _productRepository.UpdateAsync(product);
            }

            return MapToDto(new List<Product> { product }).First();
        }

        /// <summary>
        /// Actualiza los campos del producto y registra la fecha de modificación.
        /// Retorna null si el producto no existe.
        /// </summary>
        public async Task<ProductDto?> UpdateAsync(int productId, UpdateProductDto dto)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return null;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.BasePrice = dto.BasePrice;
            product.Stock = dto.Stock;
            product.IsMadeToOrder = dto.IsMadeToOrder;
            product.CategoryId = dto.CategoryId;
            product.IsActive = dto.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);
            return MapToDto(new List<Product> { product }).First();
        }

        /// <summary>
        /// Verifica existencia y delega la eliminación lógica al repositorio.
        /// Retorna false si el producto no existe.
        /// </summary>
        public async Task<bool> DeleteAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return false;
            await _productRepository.DeleteAsync(productId);
            return true;
        }

        /// <summary>
        /// Transforma entidades Product a DTOs simplificados para la API.
        /// Solo incluye colores disponibles (IsAvailable = true) y la imagen marcada como primaria.
        /// </summary>
        private List<ProductDto> MapToDto(List<Product> products)
        {
            return products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                BasePrice = p.BasePrice,
                Stock = p.Stock,
                IsMadeToOrder = p.IsMadeToOrder,
                CategoryName = p.Category?.Name,
                PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                Colors = p.Colors.Where(c => c.IsAvailable).Select(c => c.ColorName).ToList()
            }).ToList();
        }
    }
}