using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}