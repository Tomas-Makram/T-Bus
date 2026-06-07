using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class CreateTripDTO
    {
        public Guid BusId { get; set; }

        public Guid DriverId { get; set; }

        public DateTime TripDate { get; set; } = DateTime.UtcNow;

        public string FromLocation { get; set; } = string.Empty;

        public string ToLocation { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public List<CreatePaymentDTO> Payments { get; set; } = new();

        public List<CreateTahseelDTO> TahseelItems { get; set; } = new();

        public decimal Solar { get; set; } = 0m;
        public decimal? Cache { get; set; }

        public decimal TripPrice { get; set; } = decimal.MinValue;

        public decimal DriverPrice { get; set; } = decimal.MinValue;

        public string? Notes { get; set; }

        public string? CompanyName { get; set; } = "Go Bus";

        public bool inSide { get; set; } = true;
    }
}
