using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class CreateTahseelDTO
    {
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public Guid UserId { get; set; }
    }
}
