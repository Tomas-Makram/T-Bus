namespace BusinessLayer.DTOs.Trips
{
    public class GetTripDTO
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

        public decimal TotalPayments { get; set; }
        public decimal TotalTahseel { get; set; }
        public decimal TahseelRemaining { get; set; }

        public decimal TotalAvailableMoney { get; set; }

        public decimal AdminBalanceAmount { get; set; }
        public decimal DriverPaidFromPocket { get; set; }

        public decimal DriverAccountAmount { get; set; }
        public string? DriverAccountStatus { get; set; }
        public List<GetPaymentDTO> Payments { get; set; } = new();
        public List<GetTahseelDTO>? TahseelItems { get; set; } = new();

        public decimal Solar { get; set; }

        public Guid AdminId { get; set; }

        public string AdminName { get; set; } = string.Empty;

        //public decimal? Visa { get; set; }

        public decimal? Cache { get; set; }
        //public decimal? Octine { get; set; }

        public decimal TripPrice { get; set; } = decimal.MinValue;

        public decimal DriverPrice { get; set; } = decimal.MinValue;

        public bool isDone { get; set; } = false;

        public string? Notes { get; set; }

        public string? CompanyName { get; set; } = "Go Bus";

        public bool inSide { get; set; } = false;
    }
}