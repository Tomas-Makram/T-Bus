using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum RulesAccount
    {
        [Display(Name = "Admin")]
        Admin,
        [Display(Name = "Driver")]
        Driver,
    }
}
