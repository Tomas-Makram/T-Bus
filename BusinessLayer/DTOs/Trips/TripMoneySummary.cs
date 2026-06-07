using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class TripMoneySummary
    {
        public decimal TotalPayments { get; set; }
        public decimal TotalTahseel { get; set; }
        public decimal TahseelUsed { get; set; }
        public decimal TahseelRemaining { get; set; }
        public decimal CashRemaining { get; set; }
        public decimal TotalAvailableMoney { get; set; }
        public decimal DriverPaidFromPocket { get; set; }
        public decimal DriverAccountAmount { get; set; }
        public decimal AdminBalanceAmount { get; set; }
        public string? DriverAccountStatus { get; set; }
    }
}
