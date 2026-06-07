using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public class Users
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string RoleEncrypted { get; set; } = string.Empty;

        public Guid? DriverId { get; set; }

        public string? NationalIdEncrypted { get; set; }
        public string? NationalIdHash { get; set; }

        public string? PhoneNumberEncrypted { get; set; }
        public string? PhoneNumberHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public bool Login { get; set; } = false;

        public bool Blocked { get; set; } = false;
    }
}