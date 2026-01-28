using System.Text.Json.Nodes;

namespace UniversalTwin.Server.Services;

public interface ISimulationService
{
    List<JsonObject> GetRecyclerData();
    List<JsonObject> GetFactoryData();
    List<JsonObject> GetRetailData();
    
    // Mutation Methods
    void UpdateRecyclerCash(string modelName, decimal amount);
}

public class SimulationService : ISimulationService
{
    private List<JsonObject> _recyclerData;
    private List<JsonObject> _factoryData;
    private List<JsonObject> _retailData;

    public SimulationService()
    {
        _recyclerData = GenerateRecyclerData();
        _factoryData = GenerateFactoryData();
        _retailData = GenerateRetailData();
    }

    public List<JsonObject> GetRecyclerData() => _recyclerData;
    public List<JsonObject> GetFactoryData() => _factoryData;
    public List<JsonObject> GetRetailData() => _retailData;

    public void UpdateRecyclerCash(string modelName, decimal amount)
    {
        foreach (var loc in _recyclerData)
        {
            var recyclers = loc["Recyclers"] as JsonArray;
            foreach (var r in recyclers)
            {
                if (r["Model"].ToString() == modelName)
                {
                    var currentTotal = (decimal)r["TotalCash"];
                    r["TotalCash"] = currentTotal + amount;
                    return; 
                }
            }
        }
    }

    private List<JsonObject> GenerateRecyclerData()
    {
        var locations = new List<JsonObject>();
        // Rename to "Branch" to distinguish from Retail Stores
        locations.Add(CreateLocation("New York Main Branch", "1 Wall St", "Green", 2, 40.7128, -74.0060));
        locations.Add(CreateLocation("Boston Financial Center", "500 Boylston St", "Yellow", 1, 42.3601, -71.0589));
        return locations;
    }

    private JsonObject CreateLocation(string name, string address, string status, int recyclerCount, double lat, double lon)
    {
        var loc = new JsonObject();
        loc["Name"] = name;
        loc["Address"] = address;
        loc["Status"] = status;
        loc["Latitude"] = lat;
        loc["Longitude"] = lon;

        var recyclers = new JsonArray();
        for (int i = 0; i < recyclerCount; i++)
        {
            recyclers.Add(CreateRecycler($"Glory-RB-{200 + i}", i * 100));
        }
        loc["Recyclers"] = recyclers;
        return loc;
    }

    private JsonObject CreateRecycler(string model, int offset)
    {
        var r = new JsonObject();
        r["Model"] = model;
        
        var cassettes = new JsonArray();
        cassettes.Add(CreateCassette(100, 20 + offset));
        cassettes.Add(CreateCassette(50, 45 + offset));
        cassettes.Add(CreateCassette(20, 10 + offset));
        cassettes.Add(CreateCassette(10, 80 + offset));
        r["Cassettes"] = cassettes;
        
        // Calculate Total
        decimal total = cassettes.Sum(c => (decimal)c!["Denomination"]! * (int)c!["CurrentCount"]!);
        r["TotalCash"] = total;

        return r;
    }

    private JsonObject CreateCassette(decimal denom, int count)
    {
        var c = new JsonObject();
        c["Denomination"] = denom;
        c["CurrentCount"] = count;
        c["PercentFull"] = Math.Min(100, count / 20); 
        return c;
    }
    
    private List<JsonObject> GenerateFactoryData()
    {
        var lines = new List<JsonObject>();
        // Add Coords: Detroit and Stuttgart
        lines.Add(CreateLine("Detroit Assembly Plant", "Bob S.", "Running", 3, 42.3314, -83.0458));
        lines.Add(CreateLine("Stuttgart Fab", "Alice M.", "Halted", 2, 48.7758, 9.1829));
        return lines;
    }

    private JsonObject CreateLine(string name, string manager, string status, int machineCount, double lat, double lon)
    {
        var l = new JsonObject();
        l["Name"] = name;
        l["Manager"] = manager;
        l["Status"] = status;
        l["Latitude"] = lat;
        l["Longitude"] = lon;

        var machines = new JsonArray();
        var rnd = new Random();
        for (int i=0; i<machineCount; i++) 
        {
            var m = new JsonObject();
            m["Model"] = $"Welder-X{1000 + i*100}";
            m["Temperature"] = rnd.Next(80, 190);
            m["Efficiency"] = rnd.Next(40, 99);
            machines.Add(m);
        }
        l["Machines"] = machines;
        return l;
    }

    private List<JsonObject> GenerateRetailData()
    {
        var stores = new List<JsonObject>();
        stores.Add(CreateStore("Downtown MegaMart", "Sarah J.", "High", 37.7749, -122.4194));
        stores.Add(CreateStore("Suburban QuickStop", "Mike T.", "Low", 41.8781, -87.6298));
        return stores;
    }

    private JsonObject CreateStore(string name, string manager, string queue, double lat, double lon)
    {
        var s = new JsonObject();
        s["Name"] = name;
        s["Manager"] = manager;
        s["QueueLength"] = queue;
        s["Latitude"] = lat;
        s["Longitude"] = lon;
        var depts = new JsonArray();
        var rnd = new Random();
        depts.Add(CreateDept("Groceries", rnd.Next(5000, 15000), rnd.Next(50, 110)));
        depts.Add(CreateDept("Electronics", rnd.Next(2000, 8000), rnd.Next(30, 90)));
        depts.Add(CreateDept("Clothing", rnd.Next(1000, 5000), rnd.Next(80, 120)));
        s["Departments"] = depts;
        return s;
    }

    private JsonObject CreateDept(string name, int sales, int targetPct)
    {
        var d = new JsonObject();
        d["Name"] = name;
        d["DailySales"] = sales;
        d["Target"] = targetPct;
        return d;
    }
}
