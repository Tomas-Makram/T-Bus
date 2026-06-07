using Asp.Versioning;
using BusinessLayer.Filters;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TBus.Controller
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class TestController : ControllerBase
    {
        private readonly IDataCiphers _Ciphers;

        public TestController(IDataCiphers ciphers)
        {
            _Ciphers = ciphers;
        }

        [HttpGet]
        [MapToApiVersion("1.0")]
        [EncryptedRole("Admin")]
        public IActionResult Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var role = User.FindFirstValue(ClaimTypes.Role);
            var sessionId = User.FindFirstValue("session_id");
            var driverId = User.FindFirstValue("driver_id");
            var fullName = User.FindFirstValue("full_name");


            return Ok($"Swagger is working ID = {userId}, Name = {userName}, fname = {fullName}, sessionID = {sessionId}, Did = {driverId}, role = {_Ciphers.Decrypt(role!)}");
        }

        [AllowAnonymous]
        [HttpGet("hello")]
        [MapToApiVersion("1.0")]
        public IActionResult hello()
        {
            return Ok($"API is Don Working :)");
        }

    }
}