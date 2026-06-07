using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Trips
{
    public class MarkTripsDoneDTO
    {
        public List<Guid> TripIds { get; set; } = new();
    }
}
