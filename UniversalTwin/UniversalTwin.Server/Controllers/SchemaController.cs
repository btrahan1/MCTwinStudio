using Microsoft.AspNetCore.Mvc;
using UniversalTwin.Server.Models;

namespace UniversalTwin.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchemaController : ControllerBase
{
    [HttpGet("{industryId}")]
    public IActionResult GetSchema(string industryId)
    {
        if (industryId.ToLower() == "recycler") return Ok(GetRecyclerSchema());
        if (industryId.ToLower() == "factory") return Ok(GetFactorySchema());
        if (industryId.ToLower() == "retail") return Ok(GetRetailSchema());
        
        return NotFound($"Industry '{industryId}' not found.");
    }

    private IndustrySchema GetRecyclerSchema()
    {
        return new IndustrySchema
        {
            IndustryId = "recycler",
            Name = "Cash Recycler Operations",
            Entities = new List<EntityDefinition>
            {
                // Top Level: Locations (acting as the card container)
                new EntityDefinition
                {
                    Key = "Location",
                    PluralName = "Locations",
                    Layout = "Grid", 
                    CardStyle = "StatusHeavy",
                    Properties = new List<PropertyDefinition>
                    {
                        new() { Key = "Name", Label = "Location Name", Type = "Text" },
                        new() { Key = "Address", Label = "Address", Type = "Text" },
                        new() { 
                            Key = "Status", 
                            Label = "Status", 
                            Type = "Badge", 
                            Config = new() { {"Green", "success"}, {"Red", "danger"}, {"Yellow", "warning"} } 
                        }
                    },
                    // Child: Recyclers
                    ChildCollectionKey = "Recyclers",
                    Children = new EntityDefinition
                    {
                        Key = "Recycler",
                        Layout = "List",
                        Properties = new List<PropertyDefinition>
                        {
                            new() { Key = "Model", Label = "Model", Type = "Text" },
                            new() { Key = "TotalCash", Label = "Total Cash", Type = "Currency" }
                        },
                        // Child: Cassettes
                        ChildCollectionKey = "Cassettes",
                        Children = new EntityDefinition
                        {
                            Key = "Cassette",
                            Layout = "CompactList", // Dedicated Compact View
                            Properties = new List<PropertyDefinition>
                            {
                                new() { Key = "Denomination", Label = "Denom", Type = "Currency" },
                                new() { Key = "CurrentCount", Label = "Count", Type = "Number" },
                                new() { Key = "PercentFull", Label = "%", Type = "Progress" }
                            }
                        }
                    }
                }
            }
        };
    }

    private IndustrySchema GetFactorySchema()
    {
        return new IndustrySchema
        {
            IndustryId = "factory",
            Name = "Smart Factory Monitoring",
            Entities = new List<EntityDefinition>
            {
                // Top Level: Production Lines
                new EntityDefinition
                {
                    Key = "Line",
                    PluralName = "Lines",
                    Layout = "Grid",
                    CardStyle = "Industrial",
                    Properties = new List<PropertyDefinition>
                    {
                        new() { Key = "Name", Label = "Production Line", Type = "Text" },
                        new() { Key = "Manager", Label = "Supervisor", Type = "Text" },
                        new() { 
                            Key = "Status", 
                            Label = "Op Status", 
                            Type = "Badge",
                            Config = new() { {"Running", "success"}, {"Halted", "danger"}, {"Maintenance", "warning"} }
                        }
                    },
                    // Child: Machines
                    ChildCollectionKey = "Machines",
                    Children = new EntityDefinition
                    {
                        Key = "Machine",
                        Layout = "Grid", // Grid inside Grid!
                        CardStyle = "Default", 
                        Properties = new List<PropertyDefinition>
                        {
                            new() { Key = "Model", Label = "Machine Type", Type = "Text" },
                            new() { 
                                Key = "Temperature", 
                                Label = "Core Temp", 
                                Type = "Gauge", 
                                Config = new() { {"min", "0"}, {"max", "200"}, {"unit", "°C"} } 
                            },
                            new() { 
                                Key = "Efficiency", 
                                Label = "OEE", 
                                Type = "Gauge", 
                                Config = new() { {"min", "0"}, {"max", "100"}, {"unit", "%"} } 
                            }
                        }
                    }
                }
            }
        };
    }

    private IndustrySchema GetRetailSchema()
    {
        return new IndustrySchema
        {
            IndustryId = "retail",
            Name = "Retail Store Analytics",
            Entities = new List<EntityDefinition>
            {
                new EntityDefinition
                {
                    Key = "Store",
                    PluralName = "Stores",
                    Layout = "Grid",
                    CardStyle = "StatusHeavy",
                    Properties = new List<PropertyDefinition>
                    {
                        new() { Key = "Name", Label = "Store", Type = "Text" },
                        new() { Key = "Manager", Label = "Manager", Type = "Text" },
                        new() { 
                            Key = "QueueLength", 
                            Label = "Queue", 
                            Type = "Badge",
                            Config = new() { {"High", "danger"}, {"Medium", "warning"}, {"Low", "success"} }
                        }
                    },
                    // Child: Departments
                    ChildCollectionKey = "Departments",
                    Children = new EntityDefinition
                    {
                        Key = "Dept",
                        Layout = "List",
                        Properties = new List<PropertyDefinition>
                        {
                            new() { Key = "Name", Label = "Department", Type = "Text" },
                            new() { Key = "DailySales", Label = "Sales", Type = "Currency" },
                            new() { 
                                Key = "Target", 
                                Label = "Target Met", 
                                Type = "Gauge", 
                                Config = new() { {"min", "0"}, {"max", "100"}, {"unit", "%"} } 
                            }
                        }
                    }
                }
            }
        };
    }
}
