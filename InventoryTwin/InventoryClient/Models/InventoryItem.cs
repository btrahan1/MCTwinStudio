namespace InventoryClient.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public int RackIndex { get; set; }
    public int ShelfLevel { get; set; }
    public int BinIndex { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
