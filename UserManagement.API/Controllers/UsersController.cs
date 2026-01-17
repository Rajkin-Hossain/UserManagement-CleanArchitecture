using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using UserManagement.Application.Interfaces;
using UserManagement.Application.DTOs;

namespace UserManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userId = await _userService.RegisterAsync(dto, ipAddress);
            return Ok(new { UserId = userId });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProfile(Guid id)
        {
            var profile = await _userService.GetProfileAsync(id);
            return Ok(profile);
        }

        [HttpPatch("{id}/profile")]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileDto dto)
        {
            await _userService.UpdateProfileAsync(id, dto);
            return NoContent();
        }

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
        {
            await _userService.ChangePasswordAsync(id, dto);
            return NoContent();
        }

        [HttpPost("{id}/status")]
        public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusDto dto)
        {
            await _userService.SetStatusAsync(id, dto);
            return NoContent();
        }

        [HttpPost("{id}/roles/{role}")]
        public async Task<IActionResult> AssignRole(Guid id, string role)
        {
            await _userService.ManageRoleAsync(id, role, false);
            return NoContent();
        }

        [HttpDelete("{id}/roles/{role}")]
        public async Task<IActionResult> RevokeRole(Guid id, string role)
        {
            await _userService.ManageRoleAsync(id, role, true);
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] SearchCriteria criteria)
        {
            var result = await _userService.SearchAsync(criteria);
            return Ok(result);
        }
    }
}
