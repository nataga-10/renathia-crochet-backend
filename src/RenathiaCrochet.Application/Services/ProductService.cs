using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<List<ProductDto>> GetAllActiveAsync()
        {
            var products = await _productRepository.GetAllActiveAsync();

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
                Colors = p.Colors.Where(c => c.IsAvailable)
                                 .Select(c => c.ColorName)
                                 .ToList()
            }).ToList();
        }
    }
}