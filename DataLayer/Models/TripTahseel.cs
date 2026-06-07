using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class TripTahseel
    {
        [Key]
        public Guid TahseelId { get; set; } = Guid.NewGuid();

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0m;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public Trips Trip { get; set; } = null!;

        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = null!;
    }
}