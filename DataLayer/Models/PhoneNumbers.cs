using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class PhoneNumbers
    {
        [Key]
        public Guid PhoneId { get; set; } = Guid.NewGuid();

        public string Number { get; set; } = string.Empty;

        public Guid DriverId { get; set; }

        [ForeignKey(nameof(DriverId))]
        public Drivers Driver { get; set; } = null!;
    }
}
