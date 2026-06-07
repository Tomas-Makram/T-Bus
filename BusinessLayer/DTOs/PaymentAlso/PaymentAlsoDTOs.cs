using System;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.PaymentAlso
{
    public class GetPaymentAlsoDTO
    {
        public Guid PaymentAlsoId { get; set; }
        public string PaymentAlsoNote { get; set; } = string.Empty;
        public decimal PaymentAlsoPrice { get; set; }
        public DateTime CreateAt { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public Guid? DriverId { get; set; }
    }

    public class CreatePaymentAlsoDTO
    {
        [Required]
        public string PaymentAlsoNote { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "PaymentAlsoPrice must be greater than zero")]
        public decimal PaymentAlsoPrice { get; set; }

        public DateTime? CreateAt { get; set; }

        [Required]
        public Guid UserId { get; set; }
    }

    public class UpdatePaymentAlsoDTO
    {
        [Required]
        public string PaymentAlsoNote { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "PaymentAlsoPrice must be greater than zero")]
        public decimal PaymentAlsoPrice { get; set; }

        public DateTime? CreateAt { get; set; }

        [Required]
        public Guid UserId { get; set; }
    }
}
