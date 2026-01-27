using CashRecyclerServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashRecyclerServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly CashDbContext _context;

        public DashboardController(CashDbContext context)
        {
            _context = context;
        }

        [HttpGet("hierarchy")]
        public async Task<IActionResult> GetHierarchy()
        {
            var regions = await _context.Regions
                .Include(r => r.Locations)
                .ThenInclude(l => l.Recyclers)
                .ThenInclude(m => m.Cassettes)
                .ToListAsync();

            // Transform to lightweight DTOs if needed, or return raw for now (EF Core cycles might need config)
            // Let's do a quick projection to avoid Reference Loop issues
            var result = regions.Select(r => new 
            {
                r.RegionId,
                r.Name,
                TotalCash = r.Locations.Sum(l => l.Recyclers.Sum(m => m.Cassettes.Sum(c => c.CurrentCount * c.Denomination))),
                Locations = r.Locations.Select(l => new 
                {
                    l.LocationId,
                    l.Name,
                    l.Address,
                    l.Latitude,
                    l.Longitude,
                    Status = l.Recyclers.All(m => m.Status == "Online") ? "Green" : "Red",
                    Recyclers = l.Recyclers.Select(m => new 
                    {
                        m.RecyclerId,
                        m.Name,
                        m.Model,
                        m.Status,
                        TotalCash = m.Cassettes.Sum(c => c.CurrentCount * c.Denomination),
                        Cassettes = m.Cassettes.Select(c => new 
                        {
                            c.CassetteIndex,
                            c.Type,
                            c.Denomination,
                            c.CurrentCount,
                            c.MaxCapacity,
                            PercentFull = (double)c.CurrentCount / c.MaxCapacity * 100
                        })
                    })
                })
            });

            return Ok(result);
        }

        [HttpGet("all-recyclers")]
        public async Task<IActionResult> GetAllRecyclers()
        {
            var recyclers = await _context.Recyclers
                .Include(r => r.Location)
                .Include(r => r.Cassettes)
                .ToListAsync();

            var result = recyclers.Select(m => new 
            {
                m.RecyclerId,
                LocationName = m.Location?.Name ?? "Unknown",
                m.Name,
                m.Model,
                m.Status,
                TotalCash = m.Cassettes.Sum(c => c.CurrentCount * c.Denomination),
                Cassettes = m.Cassettes.Select(c => new 
                {
                    c.CassetteIndex,
                    c.Type,
                    c.Denomination,
                    c.CurrentCount,
                    c.MaxCapacity,
                    PercentFull = (double)c.CurrentCount / c.MaxCapacity * 100
                })
            });

            return Ok(result);
        }

        [HttpGet("recycler/{id}")]
        public async Task<IActionResult> GetRecycler(int id)
        {
            var machine = await _context.Recyclers
                .Include(m => m.Cassettes)
                .FirstOrDefaultAsync(m => m.RecyclerId == id);

            if (machine == null) return NotFound();

            return Ok(machine);
        }
    }
}
