using Asp.Versioning;
using BusinessLayer.DTOs.Users;
using BusinessLayer.Filters;
using BusinessLayer.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TBus.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly IAuthenticateManager _authenticateManager;

        public AuthenticateController(IAuthenticateManager authenticateManager)
        {
            _authenticateManager = authenticateManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var result = await _authenticateManager.LoginAsync(dto);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO dto)
        {
            var result = await _authenticateManager.RefreshTokenAsync(dto);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutDTO dto)
        {
            var result = await _authenticateManager.LogoutAsync(dto.UserId, dto.SessionId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("my-account/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetMyAccount(Guid userId)
        {
            var result = await _authenticateManager.GetMyAccountAsync(userId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var result = await _authenticateManager.ChangePasswordAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("update-profile")]
        [Authorize]
        [EncryptedRole("Admin")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var result = await _authenticateManager.UpdateProfileAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("admin/users")]
        [Authorize]
        [EncryptedRole("Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _authenticateManager.GetAllUsersAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("admin/create-user")]
        [Authorize]
        [EncryptedRole("Admin")]
        public async Task<IActionResult> CreateUserByAdmin([FromBody] CreateUserByAdminDTO dto)
        {
            var result = await _authenticateManager.CreateUserByAdminAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("admin/block-user/{userId}")]
        [Authorize]
        [EncryptedRole("Admin")]
        public async Task<IActionResult> BlockUser(Guid userId)
        {
            var result = await _authenticateManager.BlockUserAsync(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("admin/unblock-user/{userId}")]
        [Authorize]
        [EncryptedRole("Admin")]
        public async Task<IActionResult> UnBlockUser(Guid userId)
        {
            var result = await _authenticateManager.UnBlockUserAsync(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
