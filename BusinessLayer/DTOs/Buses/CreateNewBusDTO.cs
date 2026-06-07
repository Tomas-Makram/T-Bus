using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Buses
{
    public class CreateNewBusDTO
    {
        public string Name { get; set; } = string.Empty;

        public string PalateNumber { get; set; } = string.Empty;
    }
}
