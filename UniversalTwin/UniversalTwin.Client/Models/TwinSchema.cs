namespace UniversalTwin.Client.Models;

public class IndustrySchema
{
    public string IndustryId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<EntityDefinition> Entities { get; set; } = new();
}

public class EntityDefinition
{
    public string Key { get; set; } = "";
    public string PluralName { get; set; } = "";
    public string Layout { get; set; } = "Grid";
    public string CardStyle { get; set; } = "Default";
    
    public List<PropertyDefinition> Properties { get; set; } = new();
    
    public EntityDefinition? Children { get; set; }
    public string ChildCollectionKey { get; set; } = "";
}

public class PropertyDefinition
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "Text";
    
    public Dictionary<string, string> Config { get; set; } = new();
}
