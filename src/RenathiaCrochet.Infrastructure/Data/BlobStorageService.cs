using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using RenathiaCrochet.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Servicio de almacenamiento de imágenes en Azure Blob Storage.
    /// Lee la cadena de conexión y el nombre del contenedor desde la configuración de la app.
    /// </summary>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        /// <summary>
        /// Requiere AZURE_STORAGE_CONNECTION_STRING y AZURE_BLOB_CONTAINER en la configuración.
        /// Si no se define el contenedor, usa "product-images" por defecto.
        /// </summary>
        public BlobStorageService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(
                configuration["AZURE_STORAGE_CONNECTION_STRING"]);
            _containerName = configuration["AZURE_BLOB_CONTAINER"] ?? "product-images";
        }

        /// <summary>
        /// Sube la imagen al contenedor (lo crea con acceso público si no existe).
        /// Sobreescribe si ya existe un blob con el mismo nombre.
        /// Retorna la URL pública del blob.
        /// </summary>
        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            // Crea el contenedor con acceso público de lectura si aún no existe
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(imageStream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Extrae el nombre del archivo desde la URL y elimina el blob del contenedor.
        /// No lanza error si el blob no existe.
        /// </summary>
        public async Task DeleteImageAsync(string imageUrl)
        {
            var uri = new Uri(imageUrl);
            var fileName = Path.GetFileName(uri.LocalPath);
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}