using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.Services;
using System.Security.Claims;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Controlador de galería de fotos subidas por clientes.
    /// GET público, POST para clientes autenticados, PUT/DELETE con restricciones de rol.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GalleryController : ControllerBase
    {
        private readonly GalleryService _galleryService;

        public GalleryController(GalleryService galleryService)
        {
            _galleryService = galleryService;
        }

        /// <summary>Retorna las fotos pendientes de aprobación. Solo Admin (rol 1).</summary>
        [Authorize(Roles = "1")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var items = await _galleryService.GetPendingAsync();
            return Ok(items);
        }

        /// <summary>Retorna todas las fotos aprobadas. Acceso público.</summary>
        [HttpGet]
        public async Task<IActionResult> GetApproved()
        {
            var items = await _galleryService.GetApprovedAsync();
            return Ok(items);
        }

        /// <summary>
        /// Sube una foto a la galería. Solo clientes autenticados (rol 2).
        /// La imagen se sube a Azure Blob Storage y queda pendiente de aprobación.
        /// </summary>
        [Authorize(Roles = "2")]
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile image, [FromForm] string? caption)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var stream = image.OpenReadStream();
            var result = await _galleryService.AddAsync(userId, stream, image.FileName, caption);
            return Ok(result);
        }

        /// <summary>Aprueba una foto. Solo Admin (rol 1).</summary>
        [Authorize(Roles = "1")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var found = await _galleryService.ApproveAsync(id);
            if (!found)
                return NotFound(new { message = "Foto no encontrada" });
            return Ok(new { message = "Foto aprobada correctamente" });
        }

        /// <summary>Elimina una foto. Admin puede eliminar cualquiera; el cliente solo la suya.</summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var roleId = int.Parse(User.FindFirstValue(ClaimTypes.Role)!);
            var isAdmin = roleId == 1;

            var (found, authorized) = await _galleryService.DeleteAsync(id, userId, isAdmin);

            if (!found)
                return NotFound(new { message = "Foto no encontrada" });
            if (!authorized)
                return Forbid();

            return Ok(new { message = "Foto eliminada correctamente" });
        }
    }
}
