using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class CreateUserByAdminDTO
    {
        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;

        public RulesAccount Role { get; set; } = RulesAccount.Driver;

        public Guid? DriverId { get; set; }

        public string? NationalId { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
