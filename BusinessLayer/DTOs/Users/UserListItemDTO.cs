using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class UserListItemDTO
    {
        public Guid UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? Role { get; set; }

        public Guid? DriverId { get; set; }

        public bool Login { get; set; }

        public bool Blocked { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

    }
}
