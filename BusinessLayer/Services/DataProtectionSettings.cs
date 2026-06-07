using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Services
{
    public sealed class DataProtectionSettings
    {
        [Required]
        public string UserDataPurpose { get; set; } = "TBus.UserData";
    }
}