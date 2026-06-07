using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;

namespace BusinessLayer.Services
{
    [SupportedOSPlatform("windows")]
    public class LicenseCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, FirebaseService firebaseService, IConfiguration config)
        {
            var licenseKey = config["License:Key"];

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("License key missing");
                return;
            }

            var license = await firebaseService.GetLicenseAsync(licenseKey);

            if (license == null || !license.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Invalid or inactive license");
                return;
            }

            var currentFingerprint = DeviceInfoHelper.GetDeviceInfo().DeviceFingerprint;
            var currentTokenHash = DeviceTokenHelper.GetDeviceTokenHash();

            if (license.DeviceFingerprint != currentFingerprint ||
                license.DeviceTokenHash != currentTokenHash)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("License does not match this device");
                return;
            }

            await _next(context);
        }
    }
}
