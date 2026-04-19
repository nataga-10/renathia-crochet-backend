using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Implementación del repositorio de productos con Entity Framework Core.
    /// Usa eager loading (Include) para cargar relaciones y soft delete para eliminaciones.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Carga todos los productos activos con sus relaciones (categoría, imágenes, colores).
        /// </summary>
        public async Task<List<Product>> GetAllActiveAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Colors)
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// Filtra por categoría y estado activo. Incluye relaciones completas.
        /// </summary>
        public async Task<List<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Colors)
                .Where(p => p.IsActive && p.CategoryId == categoryId)
                .ToListAsync();
        }

        /// <summary>
        /// Busca un producto por ID sin filtrar por IsActive (permite editar productos desactivados).
        /// </summary>
        public async Task<Product?> GetByIdAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Colors)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Soft delete: en lugar de eliminar el registro, establece IsActive = false.
        /// Esto preserva el historial del producto en la base de datos.
        /// </summary>
        public async Task DeleteAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}