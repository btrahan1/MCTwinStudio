using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace UniversalTwin.Client.Services;

public interface IDataService
{
    Task<List<JsonObject>?> GetDataAsync(string industryId);
}

public class DataService : IDataService
{
    private readonly HttpClient _http;

    public DataService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<JsonObject>?> GetDataAsync(string industryId)
    {
        try 
        {
            return await _http.GetFromJsonAsync<List<JsonObject>>($"http://localhost:5223/api/data/{industryId}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
            return null;
        }
    }
}
