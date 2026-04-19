using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de almacenamiento de archivos en Azure Blob Storage.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>Sube una imagen al contenedor de Azure y retorna la URL pública del blob.</summary>
        Task<string> UploadImageAsync(Stream imageStream, string fileName);
        /// <summary>Elimina una imagen del contenedor dado su URL pública.</summary>
        Task DeleteImageAsync(string imageUrl);
    }
}
