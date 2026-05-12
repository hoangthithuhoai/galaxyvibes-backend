using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Galaxyvibes.API.Data;
using Galaxyvibes.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization; // dòng này phải có

namespace Galaxyvibes.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly string _issuer = "ThoughtGalaxyServer";
		private readonly string _audience = "ThoughtGalaxyClient";
		private readonly string _secretKey = "Chuoi_Bi_Mat_Cuc_Ky_Dai_Va_An_Toan_123!";

		public AuthController(ApplicationDbContext context)
		{
			_context = context;
		}
		[AllowAnonymous]
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return Ok(new { message = "Backend đang chạy!" });
		}
		[HttpGet("debug/download-db")]
		public IActionResult DownloadDatabase()
		{
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "galaxyvibes.db");
			if (!System.IO.File.Exists(filePath))
				return NotFound("Database file not found.");

			var bytes = System.IO.File.ReadAllBytes(filePath);
			return File(bytes, "application/octet-stream", "galaxyvibes.db");
		}

		// 1. Đăng ký
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest request)
		{
			if (_context.Users.Any(u => u.Username == request.Username))
			{
				return BadRequest(new { message = "Tên định danh này đã tồn tại!" });
			}

			var newUser = new User
			{
				Username = request.Username,
				Password = request.Password,
				Email = request.Email,
				CreatedAt = DateTime.Now
			};

			_context.Users.Add(newUser);
			await _context.SaveChangesAsync();

			return Ok(new { message = "Ghi danh thành công!" });
		}

		// 2. Đăng nhập
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest login)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u =>
				u.Username == login.Username && u.Password == login.Password);

			if (user != null)
			{
				var token = GenerateJwtToken(user.Username, user.Id, user.Role);

				// SỬA DÒNG NÀY: Trả thêm user.Role về cho Frontend
				return Ok(new
				{
					token = token,
					userId = user.Id,
					role = user.Role // <--- PHẢI CÓ DÒNG NÀY
				});
			}

			return Unauthorized(new { message = "Tọa độ hoặc mật mã không chính xác!" });
		}

		[HttpPut("update-avatar/{id}")]
		public async Task<IActionResult> UpdateAvatar(int id, IFormFile avatar)
		{
			if (avatar == null || avatar.Length == 0)
				return BadRequest(new { message = "Vui lòng chọn ảnh đại diện" });

			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
			var extension = Path.GetExtension(avatar.FileName).ToLower();
			if (!allowedExtensions.Contains(extension))
				return BadRequest(new { message = "Chỉ cho phép ảnh JPG, PNG, GIF" });

			var user = await _context.Users.FindAsync(id);
			if (user == null) return NotFound();

			var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
			if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

			var fileName = $"avatar_{id}_{Guid.NewGuid()}{extension}";
			var filePath = Path.Combine(uploadsFolder, fileName);
			using (var stream = new FileStream(filePath, FileMode.Create))
			{
				await avatar.CopyToAsync(stream);
			}

			user.AvatarUrl = $"/uploads/avatars/{fileName}";
			await _context.SaveChangesAsync();

			return Ok(new { avatarUrl = user.AvatarUrl });
		}

		// 3. Lấy thông tin cá nhân (Để đồng bộ tên Lẩu/Laura và Ảnh)
		[HttpGet("profile/{id}")]
		public async Task<IActionResult> GetProfile(int id)
		{
			// Lấy ID người đang xem (để biết họ đã follow người này chưa)
			var currentUserIdString = User.FindFirst("userId")?.Value;
			int.TryParse(currentUserIdString, out int currentUserId);

			var user = await _context.Users
				.Include(u => u.Followers) // Lấy danh sách người theo dõi mình
				.Include(u => u.Following) // Lấy danh sách người mình theo dõi
				.FirstOrDefaultAsync(u => u.Id == id);

			if (user == null) return NotFound("User not found");

			return Ok(new
			{
				id = user.Id,
				username = user.Username,
				email = user.Email,
				avatarUrl = user.AvatarUrl,
				createdAt = user.CreatedAt,
				// CẬP NHẬT: Số liệu thật từ Database
				followersCount = user.Followers.Count,
				followingCount = user.Following.Count,
				// CẬP NHẬT: Kiểm tra xem mình đã follow người này chưa
				isFollowedByMe = currentUserId > 0 && user.Followers.Any(f => f.FollowerId == currentUserId)
			});
		}

		// 4. Cập nhật thông tin (Đồng bộ ảnh đại diện)
		[HttpPut("update-profile/{id}")]
		public async Task<IActionResult> UpdateProfile(int id, [FromBody] User updateData)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null) return NotFound();

			// Cập nhật ảnh đại diện vào Database
			user.AvatarUrl = updateData.AvatarUrl;

			await _context.SaveChangesAsync();
			return Ok(new { message = "Cập nhật thành công!" });
		}

		// 5. Hàm tạo Token
		private string GenerateJwtToken(string username, int userId, string role)
		{
			var claims = new[]
			{
		new Claim(JwtRegisteredClaimNames.Sub, username),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		new Claim("userId", userId.ToString()),
        new Claim(ClaimTypes.Role, role)
	};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _issuer,
				audience: _audience,
				claims: claims,
				expires: DateTime.Now.AddHours(2),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}

	public class LoginRequest
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class RegisterRequest
	{
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}