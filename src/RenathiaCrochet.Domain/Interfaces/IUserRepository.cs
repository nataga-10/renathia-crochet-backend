using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    /// <summary>
    /// Contrato del repositorio de usuarios. Define las operaciones de acceso a datos
    /// necesarias para autenticación y gestión de cuentas.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Busca un usuario por su correo electrónico. Retorna null si no existe.</summary>
        Task<User?> GetByEmailAsync(string email);
        /// <summary>Verifica si ya existe un usuario registrado con ese correo.</summary>
        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
    }
}
