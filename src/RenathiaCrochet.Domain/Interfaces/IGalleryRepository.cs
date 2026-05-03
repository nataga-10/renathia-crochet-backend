using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    /// <summary>
    /// Contrato del repositorio de galería.
    /// </summary>
    public interface IGalleryRepository
    {
        /// <summary>Retorna solo las fotos aprobadas, ordenadas de más reciente a más antigua.</summary>
        Task<List<Gallery>> GetApprovedAsync();
        /// <summary>Retorna todas las fotos de un usuario específico.</summary>
        Task<List<Gallery>> GetByUserAsync(int userId);
        Task<Gallery?> GetByIdAsync(int galleryId);
        Task AddAsync(Gallery gallery);
        Task ApproveAsync(int galleryId);
        Task DeleteAsync(int galleryId);
    }
}
