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
	public class FollowsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public FollowsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpPost("{targetUserId}")]
		public async Task<IActionResult> ToggleFollow(int targetUserId)
		{
			var currentUserIdString = User.FindFirst("userId")?.Value;
			if (!int.TryParse(currentUserIdString, out int currentUserId)) return Unauthorized();

			if (currentUserId == targetUserId) return BadRequest(new { message = "Bạn không thể tự theo dõi chính mình!" });

			// Kiểm tra xem đã follow chưa
			var existingFollow = await _context.Follows
				.FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == targetUserId);

			if (existingFollow != null)
			{
				// Đã follow -> Bỏ follow
				_context.Follows.Remove(existingFollow);
				await _context.SaveChangesAsync();
				return Ok(new { message = "Đã hủy theo dõi", isFollowing = false });
			}
			else
			{
				// Chưa follow -> Thêm follow
				var newFollow = new Follow { FollowerId = currentUserId, FollowingId = targetUserId };
				_context.Follows.Add(newFollow);

				// ==========================================
				// TẠO THÔNG BÁO CHO NGƯỜI ĐƯỢC THEO DÕI
				// ==========================================
				var noti = new Notification
				{
					UserId = targetUserId,   // Người được follow sẽ nhận thông báo
					ActorId = currentUserId, // Mình là người gây ra hành động
					Type = "FOLLOW",
					StarId = null,           // Follow người thì không gắn với bài viết cụ thể nào
					Message = "đã cảm nhận được lực hấp dẫn và bắt đầu theo dõi vũ trụ của bạn.",
					CreatedAt = DateTime.UtcNow
				};
				_context.Notifications.Add(noti);

				await _context.SaveChangesAsync();
				return Ok(new { message = "Đã theo dõi", isFollowing = true });
			}
		}
	}
}