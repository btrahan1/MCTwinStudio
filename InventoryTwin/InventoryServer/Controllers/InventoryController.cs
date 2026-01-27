using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryServer.Data;
using InventoryServer.Models;

namespace InventoryServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
    private readonly InventoryContext _context;

    public InventoryController(InventoryContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetInventory()
    {
        return await _context.InventoryItems.ToListAsync();
    }
}
