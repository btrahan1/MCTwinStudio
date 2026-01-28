using Microsoft.AspNetCore.Mvc;
using UniversalTwin.Server.Services;

namespace UniversalTwin.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    public OperationsController(ISimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    [HttpPost("transaction")]
    public IActionResult PostTransaction([FromBody] TransactionRequest request)
    {
        // Simple logic: Update the "Glory-RB-200" (default recycler) or first found
        // In a real app, Request would have DeviceId. We'll simulate modifying 'Glory-RB-200'.
        
        decimal amount = request.Type == "Deposit" ? request.Amount : -request.Amount;
        _simulationService.UpdateRecyclerCash("Glory-RB-200", amount);
        
        return Ok(new { Message = "Transaction Processed", NewBalance = "Check Dashboard" });
    }
}

public class TransactionRequest
{
    public string Type { get; set; } = "Deposit"; // Deposit, Withdraw
    public decimal Amount { get; set; }
}
