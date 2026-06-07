using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace DataLayer.Models
{
    public class PaymentAlso
    {
        [Key]
        public Guid PaymentAlsoId { get; set; } = Guid.NewGuid();

        public string PaymentAlsoNote { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaymentAlsoPrice { get; set; } = decimal.MinValue;
    
        public DateTime CreateAt { get; set; }

        public Guid UserId { get; set; } = Guid.NewGuid();

        [ForeignKey(nameof(UserId))]
        public Users user { get; set; } = null!;
    }
}
