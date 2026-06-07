using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Users
{
    public class LogoutDTO
    {
        public Guid UserId { get; set; }

        public Guid SessionId { get; set; }
    }
}
