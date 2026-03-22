using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(Stream imageStream, string fileName);
        Task DeleteImageAsync(string imageUrl);
    }
}
