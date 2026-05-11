using Galaxyvibes.API.Data;
using Galaxyvibes.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Galaxyvibes.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StarsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public StarsController(ApplicationDbContext context)
		{
			_context = context;
		}

		// 1. API Lấy danh sách toàn bộ các vì sao để vẽ lên 3D
		// GET: api/Stars
		[HttpGet]
		public async Task<IActionResult> GetStars([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
		{
			// 1. Lấy ID người dùng đang xem trang
			var userIdString = User.FindFirst("userId")?.Value;
			int.TryParse(userIdString, out int currentUserId);

			// 2. Tính toán phân trang
			int skip = (page - 1) * pageSize;

			// 3. Đếm tổng số bài viết
			var totalItems = await _context.Stars.CountAsync();

			// 4. Lấy dữ liệu (bỏ Include Likes để tránh trùng dòng)
			//    Chỉ Include User để lấy thông tin người đăng
			var stars = await _context.Stars
				.Include(s => s.User)
				.OrderByDescending(s => s.CreatedAt)
				.Skip(skip)
				.Take(pageSize)
				.ToListAsync();

			// 5. Chuyển đổi dữ liệu (tính isLikedByMe ngay trong Select)
			var result = stars.Select(s => new
			{
				id = s.Id,
				content = s.Content,
				sentimentColor = s.SentimentColor,
				size = s.Size,
				positionX = s.PositionX,
				positionY = s.PositionY,
				positionZ = s.PositionZ,
				createdAt = s.CreatedAt,
				userId = s.UserId,
				imageUrl = s.ImageUrl,
				user = new { username = s.User?.Username, avatarUrl = s.User?.AvatarUrl },
				likeCount = s.LikeCount,
				commentCount = s.CommentCount,
				// isLikedByMe được tính bằng cách kiểm tra Likes trong DB
				isLikedByMe = currentUserId > 0 && _context.Likes.Any(l => l.StarId == s.Id && l.UserId == currentUserId)
			});

			// 6. Trả về cấu trúc phân trang
			return Ok(new
			{
				items = result,
				totalCount = totalItems,
				currentPage = page,
				totalPages = (int)Math.Ceiling((double)totalItems / pageSize)
			});
		}

		// 2. API Phóng một vì sao mới
		[HttpPost]
		public async Task<IActionResult> PostStar([FromForm] Star newStar, [FromForm] List<IFormFile>? images)
		{
			try
			{
				var userIdClaim = User.FindFirst("userId")?.Value;
				if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("Chưa đăng nhập.");

				newStar.UserId = int.Parse(userIdClaim);
				newStar.CreatedAt = DateTime.UtcNow;

				if (images != null && images.Count > 0)
				{
					var uploadedUrls = new List<string>();
					// Đảm bảo đường dẫn tuyệt đối đến thư mục wwwroot/uploads
					var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
					if (!Directory.Exists(uploadsFolder))
					{
						Directory.CreateDirectory(uploadsFolder);
					}

					foreach (var file in images)
					{
						if (file.Length > 0)
						{
							// Kiểm tra định dạng file (chỉ cho phép ảnh)
							var extension = Path.GetExtension(file.FileName).ToLower();
							if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
							{
								return BadRequest(new { message = $"Định dạng ảnh không hợp lệ: {extension}" });
							}

							var fileName = Guid.NewGuid().ToString() + extension;
							var filePath = Path.Combine(uploadsFolder, fileName);
							using (var stream = new FileStream(filePath, FileMode.Create))
							{
								await file.CopyToAsync(stream);
							}
							uploadedUrls.Add("/uploads/" + fileName);
						}
					}
					newStar.ImageUrl = string.Join(",", uploadedUrls);
				}

				_context.Stars.Add(newStar);
				await _context.SaveChangesAsync();
				return Ok(newStar);
			}
			catch (Exception ex)
			{
				var innerMessage = ex.InnerException?.Message ?? ex.Message;
				return StatusCode(500, new { message = "Lỗi server: " + innerMessage });
			}
		}

		// 3. API Lấy sao của tôi
		[HttpGet("my-stars/{userId}")]
		public async Task<ActionResult<IEnumerable<object>>> GetMyStars(int userId)
		{
			var currentUserIdString = User.FindFirst("userId")?.Value;
			int.TryParse(currentUserIdString, out int currentUserId);

			// Tương tự, bỏ Include Likes để tránh trùng lặp
			var stars = await _context.Stars
				.Include(s => s.User)
				.Where(s => s.UserId == userId)
				.OrderByDescending(s => s.CreatedAt)
				.ToListAsync();

			var result = stars.Select(s => new
			{
				id = s.Id,
				content = s.Content,
				sentimentColor = s.SentimentColor,
				size = s.Size,
				positionX = s.PositionX,
				positionY = s.PositionY,
				positionZ = s.PositionZ,
				createdAt = s.CreatedAt,
				userId = s.UserId,
				imageUrl = s.ImageUrl,
				user = new { username = s.User?.Username, avatarUrl = s.User?.AvatarUrl },
				likeCount = s.LikeCount,
				isLikedByMe = currentUserId > 0 && _context.Likes.Any(l => l.StarId == s.Id && l.UserId == currentUserId),
				isSavedByMe = currentUserId > 0 && _context.SavedStars.Any(ss => ss.StarId == s.Id && ss.UserId == currentUserId)
			});

			return Ok(result);
		}

		// 4. API Xóa vì sao
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteStar(int id)
		{
			var star = await _context.Stars
				.Include(s => s.Comments)
				.Include(s => s.Likes)
				.FirstOrDefaultAsync(s => s.Id == id);
			if (star == null)
				return NotFound(new { message = "Không tìm thấy vì sao này!" });

			// Xóa các bình luận liên quan
			if (star.Comments.Any())
				_context.Comments.RemoveRange(star.Comments);
			// Xóa các lượt thích liên quan
			if (star.Likes.Any())
				_context.Likes.RemoveRange(star.Likes);
			// Xóa các bản ghi SavedStars liên quan
			var saved = await _context.SavedStars.Where(s => s.StarId == id).ToListAsync();
			if (saved.Any())
				_context.SavedStars.RemoveRange(saved);
			// Xóa các thông báo liên quan
			var notifications = await _context.Notifications.Where(n => n.StarId == id).ToListAsync();
			if (notifications.Any())
				_context.Notifications.RemoveRange(notifications);

			_context.Stars.Remove(star);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		// 5. API Tìm kiếm bài viết
		[HttpGet("search")]
		public async Task<IActionResult> SearchStars([FromQuery] string q)
		{
			if (string.IsNullOrWhiteSpace(q))
				return Ok(new List<object>()); // trả về mảng rỗng nếu không có từ khóa

			var userIdString = User.FindFirst("userId")?.Value;
			int.TryParse(userIdString, out int currentUserId);

			var stars = await _context.Stars
				.Include(s => s.User)
				.Where(s => s.Content.Contains(q) || s.User.Username.Contains(q))
				.OrderByDescending(s => s.CreatedAt)
				.Take(20) // giới hạn 20 kết quả gần nhất
				.ToListAsync();

			var result = stars.Select(s => new
			{
				id = s.Id,
				content = s.Content,
				sentimentColor = s.SentimentColor,
				createdAt = s.CreatedAt,
				userId = s.UserId,
				imageUrl = s.ImageUrl,
				user = new { username = s.User?.Username, avatarUrl = s.User?.AvatarUrl },
				likeCount = s.LikeCount,
				commentCount = s.CommentCount,
				isLikedByMe = currentUserId > 0 && _context.Likes.Any(l => l.StarId == s.Id && l.UserId == currentUserId)
			});

			return Ok(result);
		}
	}
}