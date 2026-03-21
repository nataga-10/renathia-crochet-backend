using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(User user);
    }
}
