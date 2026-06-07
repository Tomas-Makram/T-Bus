using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BusinessLayer.DTOs.Buses
{
    public class GetBusDTO
    {
        public Guid BusId { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty;

        public string PalateNumber { get; set; } = string.Empty;

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    }
}
