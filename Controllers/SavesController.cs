using Galaxyvibes.API.Data;
using Galaxyvibes.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Galaxyvibes.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize] // Bắt buộc đăng nhập
	public class SavesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public SavesController(ApplicationDbContext context)
		{
			_context = context;
		}

		// POST: api/saves/{starId}
		[HttpPost("{starId}")]
		public async Task<IActionResult> ToggleSave(int starId)
		{
			var userIdString = User.FindFirst("userId")?.Value;
			if (!int.TryParse(userIdString, out int userId))
			{
				return Unauthorized(new { message = "Bạn chưa đăng nhập!" });
			}

			// Kiểm tra xem đã lưu chưa
			var existingSave = await _context.SavedStars
				.FirstOrDefaultAsync(s => s.StarId == starId && s.UserId == userId);

			if (existingSave != null)
			{
				// NẾU ĐÃ LƯU -> Bỏ lưu
				_context.SavedStars.Remove(existingSave);
				await _context.SaveChangesAsync();
				return Ok(new { message = "Đã bỏ lưu", isSaved = false });
			}
			else
			{
				// NẾU CHƯA LƯU -> Thêm vào danh sách lưu
				var newSave = new SavedStar { StarId = starId, UserId = userId };
				_context.SavedStars.Add(newSave);
				await _context.SaveChangesAsync();
				return Ok(new { message = "Đã lưu vào bộ sưu tập", isSaved = true });
			}
		}

		[HttpGet("my-saved")]
		public async Task<IActionResult> GetMySavedStars()
		{
			var userIdString = User.FindFirst("userId")?.Value;
			if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

			// Lấy danh sách các bài viết thông qua bảng trung gian SavedStars
			var savedStars = await _context.SavedStars
				.Include(ss => ss.Star)
				.ThenInclude(s => s.User) // Lấy thông tin người tạo bài viết
				.Where(ss => ss.UserId == userId)
				.OrderByDescending(ss => ss.SavedAt)
				.Select(ss => new
				{
					id = ss.Star.Id,
					content = ss.Star.Content,
					sentimentColor = ss.Star.SentimentColor,
					createdAt = ss.Star.CreatedAt,
					userId = ss.Star.UserId,
					imageUrl = ss.Star.ImageUrl,
					user = new { username = ss.Star.User.Username, avatarUrl = ss.Star.User.AvatarUrl },
					likeCount = ss.Star.LikeCount,
					commentCount = ss.Star.CommentCount,
					isLikedByMe = _context.Likes.Any(l => l.StarId == ss.StarId && l.UserId == userId),
					isSavedByMe = true // Hiển thị chắc chắn là đã lưu
				})
				.ToListAsync();

			return Ok(savedStars);
		}
	}
}