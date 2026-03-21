using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllActiveAsync();
        Task<List<Product>> GetByCategoryAsync(int categoryId);
    }
}
