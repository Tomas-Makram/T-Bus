using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class UpdateProfileDTO
    {
        public Guid UserId { get; set; }

        public string? FullName { get; set; }

        public string? NationalId { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
