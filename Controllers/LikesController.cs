using Galaxyvibes.API.Data;
using Galaxyvibes.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Galaxyvibes.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class LikesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public LikesController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpPost("{starId}")]
		public async Task<IActionResult> ToggleLike(int starId)
		{
			var userIdString = User.FindFirst("userId")?.Value;
			if (!int.TryParse(userIdString, out int userId))
			{
				return Unauthorized(new { message = "Bạn chưa đăng nhập!" });
			}

			var star = await _context.Stars.FindAsync(starId);
			if (star == null) return NotFound(new { message = "Không tìm thấy vì sao này." });

			var existingLike = await _context.Likes
				.FirstOrDefaultAsync(l => l.StarId == starId && l.UserId == userId);

			if (existingLike != null)
			{
				// UNLIKE
				_context.Likes.Remove(existingLike);
				await _context.SaveChangesAsync(); // Lưu thay đổi like

				// Đồng bộ lại LikeCount từ DB
				star.LikeCount = await _context.Likes.CountAsync(l => l.StarId == starId);
				await _context.SaveChangesAsync(); // Lưu count

				return Ok(new { message = "Đã thu hồi sao", likeCount = star.LikeCount, isLiked = false });
			}
			else
			{
				// LIKE
				var newLike = new Like { StarId = starId, UserId = userId };
				_context.Likes.Add(newLike);

				// Tạo thông báo cho chủ bài viết (nếu không phải tự like)
				if (star.UserId != userId)
				{
					var noti = new Notification
					{
						UserId = star.UserId,
						ActorId = userId,
						Type = "LIKE",
						StarId = starId,
						Message = "đã bắt được tần số và thả sao vào bài viết của bạn.",
						CreatedAt = DateTime.UtcNow
					};
					_context.Notifications.Add(noti);
				}

				await _context.SaveChangesAsync(); // Lưu like + notification

				// Đồng bộ lại LikeCount từ DB
				star.LikeCount = await _context.Likes.CountAsync(l => l.StarId == starId);
				await _context.SaveChangesAsync();

				return Ok(new { message = "Đã thả sao", likeCount = star.LikeCount, isLiked = true });
			}
		}
	}
}