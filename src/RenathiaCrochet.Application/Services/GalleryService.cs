using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Lógica de negocio de la galería. Coordina el repositorio con el almacenamiento en Azure Blob.
    /// </summary>
    public class GalleryService
    {
        private readonly IGalleryRepository _galleryRepository;
        private readonly IBlobStorageService _blobStorageService;

        public GalleryService(IGalleryRepository galleryRepository, IBlobStorageService blobStorageService)
        {
            _galleryRepository = galleryRepository;
            _blobStorageService = blobStorageService;
        }

        /// <summary>Retorna las fotos aprobadas mapeadas a DTO.</summary>
        public async Task<List<GalleryDto>> GetApprovedAsync()
        {
            var items = await _galleryRepository.GetApprovedAsync();
            return items.Select(MapToDto).ToList();
        }

        /// <summary>Retorna todas las fotos de un usuario.</summary>
        public async Task<List<GalleryDto>> GetByUserAsync(int userId)
        {
            var items = await _galleryRepository.GetByUserAsync(userId);
            return items.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Sube la imagen a Azure Blob Storage y registra el item en la galería pendiente de aprobación.
        /// </summary>
        public async Task<GalleryDto> AddAsync(int userId, Stream imageStream, string fileName, string? caption)
        {
            var blobName = $"gallery-{userId}-{Guid.NewGuid()}-{fileName}";
            var imageUrl = await _blobStorageService.UploadImageAsync(imageStream, blobName);

            var item = new Gallery
            {
                UserId = userId,
                ImageUrl = imageUrl,
                Caption = caption,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            await _galleryRepository.AddAsync(item);
            return MapToDto(item);
        }

        /// <summary>Aprueba una foto. Solo Admin.</summary>
        public async Task<bool> ApproveAsync(int galleryId)
        {
            var item = await _galleryRepository.GetByIdAsync(galleryId);
            if (item == null) return false;
            await _galleryRepository.ApproveAsync(galleryId);
            return true;
        }

        /// <summary>Elimina una foto. Valida que el solicitante sea Admin o el dueño del post.</summary>
        public async Task<(bool found, bool authorized)> DeleteAsync(int galleryId, int requestingUserId, bool isAdmin)
        {
            var item = await _galleryRepository.GetByIdAsync(galleryId);
            if (item == null) return (false, false);
            if (!isAdmin && item.UserId != requestingUserId) return (true, false);
            await _galleryRepository.DeleteAsync(galleryId);
            return (true, true);
        }

        private static GalleryDto MapToDto(Gallery g) => new()
        {
            GalleryId = g.GalleryId,
            UserId = g.UserId,
            UserName = g.User?.FullName,
            ImageUrl = g.ImageUrl,
            Caption = g.Caption,
            IsApproved = g.IsApproved,
            CreatedAt = g.CreatedAt
        };
    }
}
