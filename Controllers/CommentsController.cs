using Galaxyvibes.API.Data;
using Galaxyvibes.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Galaxyvibes.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class CommentsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public CommentsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet("{starId}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetComments(int starId)
		{
			var comments = await _context.Comments
				.Include(c => c.User)
				.Where(c => c.StarId == starId)
				.OrderBy(c => c.CreatedAt)
				.Select(c => new
				{
					id = c.Id,
					content = c.Content,
					createdAt = c.CreatedAt,
					userId = c.UserId, // Đã bổ sung ID của người bình luận
					user = new
					{
						id = c.User.Id, // Bổ sung luôn ID vào object User
						username = c.User.Username,
						avatarUrl = c.User.AvatarUrl
					}
				})
				.ToListAsync();

			return Ok(comments);
		}

		[HttpPost("{starId}")]
		public async Task<IActionResult> PostComment(int starId, [FromBody] CommentDto request)
		{
			var userIdString = User.FindFirst("userId")?.Value;
			if (!int.TryParse(userIdString, out int userId)) return Unauthorized();

			if (string.IsNullOrWhiteSpace(request.Content))
				return BadRequest(new { message = "Nội dung không được để trống" });

			var comment = new Comment
			{
				StarId = starId,
				UserId = userId,
				Content = request.Content,
				CreatedAt = DateTime.UtcNow
			};
			_context.Comments.Add(comment);

			var star = await _context.Stars.FindAsync(starId);
			if (star != null)
			{
				// Tạo thông báo cho chủ bài viết (nếu không tự comment)
				if (star.UserId != userId)
				{
					var noti = new Notification
					{
						UserId = star.UserId,
						ActorId = userId,
						Type = "COMMENT",
						StarId = starId,
						Message = "đã bắt được tần số và để lại bình luận trên vì sao của bạn.",
						CreatedAt = DateTime.UtcNow
					};
					_context.Notifications.Add(noti);
				}

				await _context.SaveChangesAsync(); // Lưu comment + notification

				// Đồng bộ lại CommentCount từ DB
				star.CommentCount = await _context.Comments.CountAsync(c => c.StarId == starId);
				await _context.SaveChangesAsync(); // Lưu count
			}
			else
			{
				await _context.SaveChangesAsync(); // Vẫn lưu comment dù không tìm thấy star (trường hợp hiếm)
			}

			return Ok(new { message = "Đã gửi bình luận thành công!" });
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteComment(int id)
		{
			try
			{
				// Đã sửa lại chuẩn cách lấy Token giống hệt hàm Post ở trên
				var userIdString = User.FindFirst("userId")?.Value;

				if (string.IsNullOrEmpty(userIdString))
				{
					return Unauthorized(new { message = "Bạn chưa đăng nhập hoặc Token hết hạn." });
				}

				int currentUserId = int.Parse(userIdString);

				// --- 2. TÌM BÌNH LUẬN TRONG DATABASE ---
				var comment = await _context.Comments.FindAsync(id);
				if (comment == null)
				{
					return NotFound(new { message = "Không tìm thấy bình luận này." });
				}

				// --- 3. BẢO MẬT: PHÂN QUYỀN SỞ HỮU ---
				if (comment.UserId != currentUserId)
				{
					return StatusCode(403, new { message = "Lỗi bảo mật: Bạn không có quyền xóa bình luận của người khác!" });
				}

				// --- 4. TRỪ BỘ ĐẾM CỦA BÀI VIẾT (VÌ SAO) ---
				var star = await _context.Stars.FindAsync(comment.StarId);
				if (star != null && star.CommentCount > 0)
				{
					star.CommentCount -= 1;
				}

				// --- 5. TIẾN HÀNH XÓA ---
				_context.Comments.Remove(comment);
				await _context.SaveChangesAsync();

				return Ok(new { message = "Đã xóa bình luận thành công!" });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = "Lỗi hệ thống khi xóa bình luận: " + ex.Message });
			}
		}
	}

	public class CommentDto
	{
		public string Content { get; set; }
	}
}