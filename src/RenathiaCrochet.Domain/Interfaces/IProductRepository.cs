using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    /// <summary>
    /// Contrato del repositorio de productos. Define las operaciones CRUD del catálogo.
    /// DeleteAsync realiza eliminación lógica (soft delete), no borra el registro.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>Retorna solo los productos con IsActive = true, incluyendo categoría, imágenes y colores.</summary>
        Task<List<Product>> GetAllActiveAsync();
        /// <summary>Filtra productos activos por categoría.</summary>
        Task<List<Product>> GetByCategoryAsync(int categoryId);
        Task<Product?> GetByIdAsync(int productId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        /// <summary>Realiza soft delete: establece IsActive = false sin eliminar el registro.</summary>
        Task DeleteAsync(int productId);
    }
}