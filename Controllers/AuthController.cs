// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TaskManagementApi.Models;

namespace YourProjectName.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<UserModel> _userManager;
		private readonly SignInManager<UserModel> _signInManager;

		private readonly IConfiguration _configuration;

		public AuthController(UserManager<UserModel> userManager, IConfiguration configuration, SignInManager<UserModel> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_configuration = configuration;
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest model)
		{
			var user = await _userManager.FindByNameAsync(model.Username);

			if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
			{
				return Unauthorized(new { Message = "Invalid credentials" });
			}

			var token = GenerateJwtToken(user);

			return Ok(new { Token = token, User = user });
		}

		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest model)
		{
			var existingUser = await _userManager.FindByNameAsync(model.Username);

			if (existingUser != null)
			{
				return BadRequest(new { Message = "Username is already taken" });
			}

			var newUser = new UserModel { UserName = model.Username, Email = model.Email, Role = "Admin" }; // Default role is 'User'
			var result = await _userManager.CreateAsync(newUser, model.Password);

			if (result.Succeeded)
			{
				// Assign role during registration
				await _userManager.AddToRoleAsync(newUser, newUser.Role);

				await _signInManager.SignInAsync(newUser, isPersistent: false);

				var token = GenerateJwtToken(newUser);
				return Ok(new { Token = token });
			}
			else
			{
				return BadRequest(new { Message = "Registration failed", Errors = result.Errors });
			}
		}

		[HttpPost("signup")]
		public async Task<IActionResult> Signup([FromBody] Auth0User model)
		{
			// Check if the user already exists in your database
			var existingUser = await _userManager.FindByNameAsync(model.Username);

			if (existingUser != null)
			{
				return Ok(new { Token = GenerateJwtToken(existingUser), Message = "User already exists" });
			}

			// Create a new user in your database
			var newUser = new UserModel
			{
				Id = model.UserId,
				UserName = model.Username,
				Email = model.Email,
				Role = "User"
			};

			var result = await _userManager.CreateAsync(newUser);

			if (result.Succeeded)
			{
				// Assign the user role
				await _userManager.AddToRoleAsync(newUser, newUser.Role);

				var token = GenerateJwtToken(newUser);

				return Ok(new { Token = token, Message = "User registered successfully" });
			}
			else
			{
				var token = GenerateJwtToken(existingUser);

				return Ok(new { Token = token, Message = "User already exists" });
			}
		}

		private string GenerateJwtToken(UserModel user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
				new Claim(ClaimTypes.Name, user.UserName),
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Include UserId claim
			        new Claim(ClaimTypes.Role, user.Role), // Include Role claim	
				}),
				// Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.UserName), new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()) }),
				Expires = DateTime.UtcNow.AddHours(1), // Adjust as needed
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
				Issuer = _configuration["Jwt:Issuer"],
				Audience = _configuration["Jwt:Audience"]
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);

		}


	}


}
