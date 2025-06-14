using Microsoft.AspNetCore.Mvc;
using LMS.Core.Interfaces.Services;
using LMS.Application.DTOs;
using LMS.Core.Models;

namespace LMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userService.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("User with this email already exists");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = dto.Password,
                Role = dto.Role
            };

            var createdUser = await _userService.CreateUserAsync(user);
            var token = await _userService.GenerateJwtTokenAsync(createdUser);

            return Ok(new
            {
                Token = token,
                User = new UserDto
                {
                    Id = createdUser.Id,
                    FirstName = createdUser.FirstName,
                    LastName = createdUser.LastName,
                    Email = createdUser.Email,
                    Role = createdUser.Role
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _userService.ValidateUserCredentialsAsync(dto.Email, dto.Password);
            if (!isValid)
                return Unauthorized("Invalid credentials");

            var user = await _userService.GetUserByEmailAsync(dto.Email);
            var token = await _userService.GenerateJwtTokenAsync(user);

            return Ok(new
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = user.Role
                }
            });
        }
    }
}