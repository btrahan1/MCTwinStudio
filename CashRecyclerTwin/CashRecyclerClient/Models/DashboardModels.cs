
namespace CashRecyclerClient.Models
{
    public class RegionDto
    {
        public int RegionId { get; set; }
        public string Name { get; set; } = "";
        public decimal TotalCash { get; set; }
        public List<LocationDto> Locations { get; set; } = new();
    }

    public class LocationDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Status { get; set; } = "Gray";
        public List<RecyclerDto> Recyclers { get; set; } = new();
    }

    public class RecyclerDto
    {
        public int RecyclerId { get; set; }
        public string LocationName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalCash { get; set; }
        public List<CassetteDto> Cassettes { get; set; } = new();
    }

    public class CassetteDto
    {
        public int CassetteIndex { get; set; }
        public string Type { get; set; } = "";
        public decimal Denomination { get; set; }
        public int CurrentCount { get; set; }
        public int MaxCapacity { get; set; }
        public double PercentFull { get; set; }
    }
}
