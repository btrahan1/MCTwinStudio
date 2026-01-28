using System.Net.Http.Json;
using UniversalTwin.Client.Models;

namespace UniversalTwin.Client.Services;

public interface ISchemaService
{
    Task<IndustrySchema?> GetSchemaAsync(string industryId);
}

public class SchemaService : ISchemaService
{
    private readonly HttpClient _http;

    public SchemaService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IndustrySchema?> GetSchemaAsync(string industryId)
    {
        try 
        {
            return await _http.GetFromJsonAsync<IndustrySchema>($"http://localhost:5223/api/schema/{industryId}");
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error fetching schema: {ex.Message}");
            return null;
        }
    }
}
