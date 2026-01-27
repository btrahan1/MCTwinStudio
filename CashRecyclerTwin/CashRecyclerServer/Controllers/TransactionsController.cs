using CashRecyclerServer.Data;
using CashRecyclerServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashRecyclerServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly CashDbContext _context;
        private readonly IHubContext<CashHub> _hubContext;

        public TransactionsController(CashDbContext context, IHubContext<CashHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public class TransactionRequest
        {
            public int RecyclerId { get; set; }
            public string Type { get; set; } = "Deposit"; // Deposit, Withdrawal, CIT_Pickup, CIT_Delivery
            public decimal Amount { get; set; } // For logging mostly
            public Dictionary<decimal, int> DenomCounts { get; set; } = new(); // Denom -> Count
        }

        [HttpPost]
        public async Task<IActionResult> PostTransaction([FromBody] TransactionRequest request)
        {
            var recycler = await _context.Recyclers
                .Include(r => r.Cassettes)
                .FirstOrDefaultAsync(r => r.RecyclerId == request.RecyclerId);

            if (recycler == null) return NotFound("Recycler not found");

            // 1. Update Cassettes
            foreach (var cassette in recycler.Cassettes.Where(c => c.Type == "Recycle"))
            {
                if (request.Type == "CIT_Pickup")
                {
                     // Empty near zero
                     cassette.CurrentCount = 0; 
                }
                else if (request.Type == "CIT_Delivery")
                {
                     // Fill to max
                     cassette.CurrentCount = cassette.MaxCapacity;
                }
                else if (request.DenomCounts.ContainsKey(cassette.Denomination))
                {
                    int countChange = request.DenomCounts[cassette.Denomination];
                    
                    if (request.Type == "Deposit")
                        cassette.CurrentCount += countChange;
                    else if (request.Type == "Withdrawal")
                        cassette.CurrentCount -= countChange;
                        
                    // Clamp
                    if (cassette.CurrentCount < 0) cassette.CurrentCount = 0;
                    if (cassette.CurrentCount > cassette.MaxCapacity) cassette.CurrentCount = cassette.MaxCapacity;
                }
            }

            // 2. Calculate Total for Log
            decimal total = 0;
            if (request.Type == "CIT_Pickup" || request.Type == "CIT_Delivery")
            {
                total = recycler.Cassettes.Sum(c => c.CurrentCount * c.Denomination); // Just snapshot total? or delta?
                // For simplicity, let's just log 0 for CIT bulk ops in this version, or estimate.
            }
            else
            {
                total = request.DenomCounts.Sum(kv => kv.Key * kv.Value);
            }

            // 3. Log Transaction
            var txn = new Transaction
            {
                RecyclerId = request.RecyclerId,
                Timestamp = DateTime.Now,
                Type = request.Type,
                TotalAmount = total
            };
            _context.Transactions.Add(txn);

            await _context.SaveChangesAsync();

            // 4. Notify Clients (SignalR)
            // DISABLED: Letting the Background Watcher handle this to simulate "Sidecar" pattern.
            // await _hubContext.Clients.All.SendAsync("DashboardUpdate");

            return Ok(new { Message = "Transaction Processed", NewState = recycler });
        }
    }
}
