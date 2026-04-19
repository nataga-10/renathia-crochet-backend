using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Repositorio de ordenes y carrito de compras.
    /// Implementa IOrderRepository para acceder a la BD.
    /// Maneja tanto el carrito (orden PendingPayment) como
    /// el historial de pedidos del usuario.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Busca el carrito activo del usuario.
        /// El carrito es una Order con Status = "PendingPayment".
        /// Si no existe, retorna null (no tiene carrito aun).
        /// </summary>
        public async Task<Order?> GetCartByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.Images)
                .Where(o => o.UserId == userId && o.Status == "PendingPayment")
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Busca un pedido especifico por su ID.
        /// Incluye los items, tracking y datos del usuario.
        /// </summary>
        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Tracking)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        /// <summary>
        /// Trae todos los pedidos historicos de un usuario.
        /// Ordenados del mas reciente al mas antiguo.
        /// Usado en HU-09 (Ver estado pedido).
        /// </summary>
        public async Task<List<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Tracking)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Crea una nueva orden (carrito) en la base de datos.
        /// Se llama cuando el usuario agrega su PRIMER producto al carrito.
        /// </summary>
        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Actualiza una orden existente.
        /// Se usa al finalizar la compra (cambia el Status)
        /// o al recalcular el subtotal y total del carrito.
        /// </summary>
        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Agrega un nuevo producto al carrito (nueva fila en OrderItems).
        /// Se llama cuando el usuario agrega un producto que NO estaba en el carrito.
        /// </summary>
        public async Task AddItemAsync(OrderItem item)
        {
            await _context.OrderItems.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Actualiza la cantidad de un producto ya existente en el carrito.
        /// Se llama en HU-06 (Actualizar carrito).
        /// </summary>
        public async Task UpdateItemAsync(OrderItem item)
        {
            _context.OrderItems.Update(item);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Elimina un producto del carrito.
        /// Se llama en HU-07 (Eliminar del carrito).
        /// Si era el ultimo producto, el carrito queda vacio.
        /// </summary>
        public async Task RemoveItemAsync(int orderItemId)
        {
            var item = await _context.OrderItems.FindAsync(orderItemId);
            if (item != null)
            {
                _context.OrderItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Agrega un nuevo registro al historial de estados del pedido.
        /// Cada vez que el pedido cambia de estado se crea un nuevo tracking.
        /// Ejemplo: PendingPayment -> PaymentReceived -> InProduction -> Shipped
        /// </summary>
        public async Task AddTrackingAsync(OrderTracking tracking)
        {
            await _context.OrderTracking.AddAsync(tracking);
            await _context.SaveChangesAsync();
        }
    }
}
