using System;
using System.Collections.Generic;
using System.Text;

using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface IOrderRepository
    {
        //
        Task<Order?> GetCartByUserIdAsync(int userId);
        Task<Order?> GetByIdAsync(int orderId);
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task AddItemAsync(OrderItem item);
        Task UpdateItemAsync(OrderItem item);
        Task RemoveItemAsync(int orderItemId);
        Task AddTrackingAsync(OrderTracking tracking);
    }
}