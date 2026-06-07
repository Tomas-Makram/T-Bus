using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public sealed class TripViewDTO
    {
        public Guid TripId { get; set; }
        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string BusPalateNumber { get; set; } = string.Empty;
        public Guid DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverNationalId { get; set; } = string.Empty;
        public DateTime TripDate { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public decimal? Cache { get; set; }
        public decimal TripPrice { get; set; }
        public decimal DriverPrice { get; set; }
        public decimal Solar { get; set; }
        public Guid AdminId { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public decimal TotalPayments { get; set; }
        public decimal TotalTahseel { get; set; }
        public decimal TahseelRemaining { get; set; }
        public decimal TotalAvailableMoney { get; set; }
        public decimal DriverPaidFromPocket { get; set; }
        public decimal DriverAccountAmount { get; set; }
        public decimal AdminBalanceAmount { get; set; }
        public string DriverAccountStatus { get; set; } = string.Empty;
        public bool isDone { get; set; }
        public bool inSide { get; set; }
        public string? Notes { get; set; }
        public string? CompanyName { get; set; }

        public bool CanBePermanentlyDeleted { get; set; }
        public string PermanentDeleteReason { get; set; } = string.Empty;
        public int PermanentDeleteWindowHours { get; set; }
        public DateTime? PermanentDeleteAllowedFrom { get; set; }
        public DateTime? PermanentDeleteAllowedUntil { get; set; }

        public List<GetPaymentDTO> Payments { get; set; } = new();
        public List<GetTahseelDTO> TahseelItems { get; set; } = new();
    }
}
