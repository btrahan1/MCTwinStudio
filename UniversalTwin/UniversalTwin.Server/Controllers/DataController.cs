using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using UniversalTwin.Server.Services;

namespace UniversalTwin.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly ISimulationService _simulationService;

    public DataController(ISimulationService simulationService)
    {
        _simulationService = simulationService;
    }

    [HttpGet("{industryId}")]
    public IActionResult GetData(string industryId)
    {
        if (industryId.ToLower() == "recycler") return Ok(_simulationService.GetRecyclerData());
        if (industryId.ToLower() == "factory") return Ok(_simulationService.GetFactoryData());
        if (industryId.ToLower() == "retail") return Ok(_simulationService.GetRetailData());
        return NotFound();
    }
}

