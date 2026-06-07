using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class Trips
    {
        [Key]
        public Guid TripId { get; set; } = Guid.NewGuid();

        public Guid BusId { get; set; } = new Guid();

        [ForeignKey(nameof(BusId))]
        public Buses Bus { get; set; } = null!;

        public Guid DriverId { get; set; } = new Guid();

        [ForeignKey(nameof(DriverId))]
        public Drivers Driver { get; set; } = null!;

        public DateTime TripDate { get; set; } = DateTime.UtcNow;

        public string FromLocation { get; set; } = string.Empty;

        public string ToLocation { get; set; } = string.Empty;

        public ICollection<Payments> Payments { get; set; } = new List<Payments>();

        public ICollection<TripTahseel> TahseelItems { get; set; } = new List<TripTahseel>();

        public Guid AdminId { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(AdminId))]
        public Users User { get; set; }= null!;

        //[Column(TypeName = "decimal(18,2)")]
        //public decimal? Visa { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cache { get; set; }

        //[Column(TypeName = "decimal(18,2)")]
        //public decimal? Octine { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Solar { get; set; } = 0m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TripPrice { get; set; } = decimal.MinValue;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DriverPrice { get; set; } = decimal.MinValue;
    
        public bool IsDone { get; set; } = false;

        public string? Notes { get; set; }

        public string? CompanyName { get; set; } = "Go Bus";

        public bool inSide { get; set; } = false;
    }
}