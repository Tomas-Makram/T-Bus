using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public class Buses
    {
        [Key]
        public Guid BusId { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string PalateNumber { get; set; } = string.Empty;

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    }
}
