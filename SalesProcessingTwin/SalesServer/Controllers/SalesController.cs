using Microsoft.AspNetCore.Mvc;
using SalesServer.Data;

namespace SalesServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly SalesContext _context;

        public SalesController(SalesContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] SaleRequest request)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // 1. Record Sale
                var sale = new Sale
                {
                    ProductSku = request.Sku,
                    Quantity = request.Quantity,
                    SoldAt = DateTime.Now,
                    IsProcessed = false
                };

                _context.Sales.Add(sale);

                // 2. Decrement Inventory (Simple Logic: Find first bin with items)
                var inventoryItem = _context.InventoryItems
                    .FirstOrDefault(i => i.ProductSku == request.Sku && i.Quantity > 0);

                if (inventoryItem != null)
                {
                    inventoryItem.Quantity -= request.Quantity;
                    if (inventoryItem.Quantity < 0) inventoryItem.Quantity = 0; // Prevent negative
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "Sale Recorded & Inventory Updated", Id = sale.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class SaleRequest
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
    }
}
