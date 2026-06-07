using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class GetPaymentDTO
    {
        public Guid PaymentId { get; set; }

        public decimal Amount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public Guid TripId { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

       // public bool isOCtine { get; set; } = false;

        public string PaymentType { get; set; } = string.Empty;

    }
}
