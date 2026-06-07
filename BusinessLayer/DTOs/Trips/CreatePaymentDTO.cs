using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class CreatePaymentDTO
    {
        public decimal Amount { get; set; }

        public string Notes { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        //public bool isOCtine { get; set; } = false;

        public string PaymentType { get; set; } = string.Empty;


    }
}
