using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class RefreshTokenDTO
    {
        public Guid SessionId { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
    }
}
