using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Implementación EF Core del repositorio de galería.
    /// </summary>
    public class GalleryRepository : IGalleryRepository
    {
        private readonly AppDbContext _context;

        public GalleryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Gallery>> GetApprovedAsync()
        {
            return await _context.Gallery
                .Include(g => g.User)
                .Where(g => g.IsApproved)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Gallery>> GetPendingAsync()
        {
            return await _context.Gallery
                .Include(g => g.User)
                .Where(g => !g.IsApproved)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Gallery>> GetByUserAsync(int userId)
        {
            return await _context.Gallery
                .Include(g => g.User)
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        public async Task<Gallery?> GetByIdAsync(int galleryId)
        {
            return await _context.Gallery
                .Include(g => g.User)
                .FirstOrDefaultAsync(g => g.GalleryId == galleryId);
        }

        public async Task AddAsync(Gallery gallery)
        {
            await _context.Gallery.AddAsync(gallery);
            await _context.SaveChangesAsync();
        }

        public async Task ApproveAsync(int galleryId)
        {
            var item = await _context.Gallery.FindAsync(galleryId);
            if (item != null)
            {
                item.IsApproved = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int galleryId)
        {
            var item = await _context.Gallery.FindAsync(galleryId);
            if (item != null)
            {
                _context.Gallery.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
