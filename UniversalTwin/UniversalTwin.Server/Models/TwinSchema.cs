namespace UniversalTwin.Server.Models;

public class IndustrySchema
{
    public string IndustryId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<EntityDefinition> Entities { get; set; } = new();
}

public class EntityDefinition
{
    public string Key { get; set; } = ""; // e.g. "Recycler", "Location"
    public string PluralName { get; set; } = "";
    public string Layout { get; set; } = "Grid"; // Grid, List, Map
    public string CardStyle { get; set; } = "Default"; // Default, Industrial, StatusHeavy
    
    // Properties to display on the card
    public List<PropertyDefinition> Properties { get; set; } = new();
    
    // Nested children (e.g. Recycler -> Cassettes)
    public EntityDefinition? Children { get; set; } 
    public string ChildCollectionKey { get; set; } = ""; // e.g. "Cassettes"
}

public class PropertyDefinition
{
    public string Key { get; set; } = ""; // JSON property name (e.g. "temperature")
    public string Label { get; set; } = ""; // Display Name
    public string Type { get; set; } = "Text"; // Text, Number, Currency, Badge, Gauge, Progress
    
    // Optional Config
    public Dictionary<string, string> Config { get; set; } = new();
}
