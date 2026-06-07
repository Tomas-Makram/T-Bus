using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class Drivers
    {
        [Key]
        public Guid DriverId { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;

        public ICollection<PhoneNumbers> PhoneNumbers { get; set; } = new List<PhoneNumbers>();
        
        public DateTime JouinAt { get; set; } = DateTime.UtcNow;
    }
}