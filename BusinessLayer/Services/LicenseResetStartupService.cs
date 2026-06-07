using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace BusinessLayer.Services
{
    [SupportedOSPlatform("windows")]
    public class LicenseResetStartupService : IHostedService
    {
        private readonly FirebaseService _firebaseService;
        private readonly IConfiguration _config;
        private readonly ILogger<LicenseResetStartupService> _logger;

        public LicenseResetStartupService(FirebaseService firebaseService, IConfiguration config, ILogger<LicenseResetStartupService> logger)
        {
            _firebaseService = firebaseService;
            _config = config;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var licenseKey = _config["License:Key"];

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                _logger.LogWarning("License key missing. Reset startup skipped.");
                return;
            }

            try
            {
                await _firebaseService.ResetLicenseDeviceIfNeededAsync(licenseKey);

                _logger.LogInformation("License reset startup check completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "License reset startup check failed.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}