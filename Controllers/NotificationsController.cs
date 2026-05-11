using Galaxyvibes.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Galaxyvibes.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class NotificationsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public NotificationsController(ApplicationDbContext context)
		{
			_context = context;
		}

		// Lấy danh sách thông báo của tôi
		[HttpGet]
		public async Task<IActionResult> GetMyNotifications()
		{
			var userIdString = User.FindFirst("userId")?.Value;
			if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

			var notifications = await _context.Notifications
				.Include(n => n.Actor)
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.CreatedAt)
				.Select(n => new
				{
					id = n.Id,
					type = n.Type,
					message = n.Message,
					isRead = n.IsRead,
					createdAt = n.CreatedAt,
					starId = n.StarId,
					actor = new { id = n.Actor.Id, username = n.Actor.Username, avatarUrl = n.Actor.AvatarUrl }
				})
				.ToListAsync();

			return Ok(notifications);
		}
	}
}