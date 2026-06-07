using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataLayer.Models
{
    public class Payments
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0.0m;

        public string Notes { get; set; } = string.Empty;

        public Guid TripId { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(TripId))]
        public Trips Trip { get; set; } = null!;

        public Guid UserId { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(UserId))]
        public Users User { get; set; } = null!;

        public bool isOCtine { get; set; } = false;

        public string PaymentType { get; set; } = string.Empty;
    }
}
