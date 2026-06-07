using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class LoginDTO
    {
        public string EmailOrPhoneOrUsernameOrNationalId { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
