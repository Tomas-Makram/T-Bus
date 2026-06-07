using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class GetTahseelDTO
    {
        public Guid TahseelId { get; set; }
        public decimal Amount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid TripId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}
