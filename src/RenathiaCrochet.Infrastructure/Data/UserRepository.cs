using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Implementación del repositorio de usuarios con Entity Framework Core.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Busca el primer usuario que coincida con el correo dado.</summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>Consulta eficiente (AnyAsync) para verificar duplicados de correo antes de registrar.</summary>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}