using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllActiveAsync();
        Task<List<Product>> GetByCategoryAsync(int categoryId);
        Task<Product?> GetByIdAsync(int productId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int productId);
    }
}