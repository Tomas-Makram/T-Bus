namespace BusinessLayer.DTOs.Drivers
{
    public class GetDriverDTO
    {
        public Guid DriverId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;

        public List<string> PhoneNumbers { get; set; } = new();

        public DateTime JouinAt { get; set; }
    }
}