using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLayer.DTOs.Friebase
{
    public class LicenseModelDTO
    {
        public string LicenseKey { get; set; } = "";
        public string DeviceFingerprint { get; set; } = "";
        public string DeviceTokenHash { get; set; } = "";
        public bool IsActive { get; set; }

        public bool Reset { get; set; } = false;
    }
}
