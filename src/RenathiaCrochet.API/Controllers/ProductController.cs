using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.Services;

namespace RenathiaCrochet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductsController(ProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllActiveAsync();

            if (!products.Any())
                return Ok(new { message = "No hay productos disponibles por el momento" });

            return Ok(products);
        }
        //Filtro de productos
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await _productService.GetByCategoryAsync(categoryId);

            if (!products.Any())
                return Ok(new { message = "No hay productos disponibles en esta categoría" });

            return Ok(products);
        }
    }
}