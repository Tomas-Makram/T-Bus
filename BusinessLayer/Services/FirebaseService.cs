using BusinessLayer.DTOs.Friebase;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace BusinessLayer.Services
{
    public class FirebaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _databaseUrl;

        public FirebaseService(IConfiguration config, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _databaseUrl = config["Firebase:databaseURL"]!;
        }

        public async Task<LicenseModelDTO?> GetLicenseAsync(string licenseKey)
        {
            var url = $"{_databaseUrl}/licenses/{licenseKey}.json";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json) || json == "null")
                return null;

            return JsonConvert.DeserializeObject<LicenseModelDTO>(json);
        }

        [SupportedOSPlatform("windows")]
        public async Task ResetLicenseDeviceIfNeededAsync(string licenseKey)
        {
            var license = await GetLicenseAsync(licenseKey);

            if (license == null)
                return;

            if (!license.Reset)
                return;

            var device = DeviceInfoHelper.GetDeviceInfo();

            license.DeviceFingerprint = device.DeviceFingerprint;
            license.DeviceTokenHash = DeviceTokenHelper.GetDeviceTokenHash();
            license.IsActive = true;
            license.Reset = false;

            await SaveLicenseAsync(license);
        }

        public async Task SaveLicenseAsync(LicenseModelDTO model)
        {
            var url = $"{_databaseUrl}/licenses/{model.LicenseKey}.json";

            var json = JsonConvert.SerializeObject(model);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content);

            response.EnsureSuccessStatusCode();
        }
    }
}
