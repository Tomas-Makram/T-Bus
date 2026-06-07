using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Management;
using System.Security.Cryptography;
using System.Text;

// dotnet add package System.Management
// dotnet add package Google.Cloud.Firestore
namespace BusinessLayer.Services
{
    [SupportedOSPlatform("windows")]
    public static class DeviceTokenHelper
    {
        private const string RegistryPath = @"SOFTWARE\MySecureApp";
        private const string RegistryKey = "DeviceToken";

        public static string GetOrCreateDeviceToken()
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);

            var existingToken = key.GetValue(RegistryKey)?.ToString();

            if (!string.IsNullOrWhiteSpace(existingToken))
                return existingToken;

            var newToken = Guid.NewGuid().ToString();

            key.SetValue(RegistryKey, newToken);

            return newToken;
        }

        public static string GetDeviceTokenHash()
        {
            var token = GetOrCreateDeviceToken();

            using var sha = SHA256.Create();

            var bytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }
    }

    [SupportedOSPlatform("windows")]
    public static class DeviceInfoHelper
    {
        public static DeviceInfo GetDeviceInfo()
        {
            var info = new DeviceInfo
            {
                MachineGuid = GetMachineGuid(),

                MotherboardSerial =
                    GetWmiValue(
                        "Win32_BaseBoard",
                        "SerialNumber"),

                BiosSerial =
                    GetWmiValue(
                        "Win32_BIOS",
                        "SerialNumber"),

                CpuId =
                    GetWmiValue(
                        "Win32_Processor",
                        "ProcessorId"),

                DiskSerial =
                    GetWmiValue(
                        "Win32_PhysicalMedia",
                        "SerialNumber"),

                ComputerName =
                    Environment.MachineName,

                UserName =
                    Environment.UserName,

                OSVersion =
                    Environment.OSVersion.ToString()
            };

            string rawFingerprint =
                $"{info.MachineGuid}|{info.MotherboardSerial}|{info.BiosSerial}|{info.CpuId}|{info.DiskSerial}";

            info.DeviceFingerprint =
                Sha256(rawFingerprint);

            return info;
        }

        private static string GetMachineGuid()
        {
            try
            {
                using var key =
                    Registry.LocalMachine.OpenSubKey(
                        @"SOFTWARE\Microsoft\Cryptography");

                return key?
                    .GetValue("MachineGuid")?
                    .ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string GetWmiValue(
            string className,
            string propertyName)
        {
            try
            {
                using var searcher =
                    new ManagementObjectSearcher(
                        $"SELECT {propertyName} FROM {className}");

                foreach (ManagementObject obj in searcher.Get())
                {
                    var value =
                        obj[propertyName]?
                        .ToString()?
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(value) &&
                        !value.Contains("To be filled",
                            StringComparison.OrdinalIgnoreCase) &&
                        !value.Contains("Default string",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return value;
                    }
                }
            }
            catch
            {
            }

            return "";
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();

            var bytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(input));

            return Convert.ToHexString(bytes);
        }
    }

    public class DeviceInfo
    {
        public string MachineGuid { get; set; } = "";

        public string MotherboardSerial { get; set; } = "";

        public string BiosSerial { get; set; } = "";

        public string CpuId { get; set; } = "";

        public string DiskSerial { get; set; } = "";

        public string ComputerName { get; set; } = "";

        public string UserName { get; set; } = "";

        public string OSVersion { get; set; } = "";

        public string DeviceFingerprint { get; set; } = "";
    }
}