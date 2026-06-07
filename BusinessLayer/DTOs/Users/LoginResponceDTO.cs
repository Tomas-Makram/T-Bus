using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class LoginResponceDTO
    {
        public Guid UserID { get; set; }

        public Guid SessionId { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime ExpireAt { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpireAt { get; set; }
    }
}
