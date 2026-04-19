using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Controlador de productos. Gestiona el catálogo: creación, consulta, actualización y eliminación (soft delete).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Retorna todos los productos activos del catálogo.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllActiveAsync();
            if (!products.Any())
                return Ok(new { message = "No hay productos disponibles por el momento" });
            return Ok(products);
        }

        /// <summary>
        /// Retorna un producto por su ID. Retorna 404 si no existe.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Producto no encontrado" });
            return Ok(product);
        }

        /// <summary>
        /// Retorna los productos activos que pertenecen a una categoría específica.
        /// </summary>
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await _productService.GetByCategoryAsync(categoryId);
            if (!products.Any())
                return Ok(new { message = "No hay productos disponibles en esta categoría" });
            return Ok(products);
        }

        /// <summary>
        /// Crea un nuevo producto. Acepta multipart/form-data para incluir una imagen opcional.
        /// La imagen se sube a Azure Blob Storage si se proporciona.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateProductDto dto, IFormFile? image)
        {
            Stream? imageStream = null;
            string? fileName = null;

            // Extraer el stream y nombre del archivo si se adjuntó una imagen
            if (image != null)
            {
                imageStream = image.OpenReadStream();
                fileName = image.FileName;
            }

            var result = await _productService.CreateAsync(dto, imageStream, fileName);
            return CreatedAtAction(nameof(GetById), new { id = result.ProductId }, result);
        }

        /// <summary>
        /// Actualiza los datos de un producto existente. Retorna 404 si no existe.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var result = await _productService.UpdateAsync(id, dto);
            if (result == null)
                return NotFound(new { message = "Producto no encontrado" });
            return Ok(result);
        }

        /// <summary>
        /// Realiza la eliminación lógica (soft delete) de un producto.
        /// El producto no se borra físicamente, solo se desactiva (IsActive = false).
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Producto no encontrado" });
            return Ok(new { message = "Producto eliminado correctamente" });
        }
    }
}
