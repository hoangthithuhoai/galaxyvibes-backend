using Galaxyvibes.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace Galaxyvibes.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")] // CHỈ ADMIN MỚI VÀO ĐƯỢC ĐÂY
	public class AdminController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public AdminController(ApplicationDbContext context) { _context = context; }

		// 1. API XUẤT BÁO CÁO THỐNG KÊ
		[HttpGet("stats")]
		public async Task<IActionResult> GetSystemStats()
		{
			var stats = new
			{
				TotalUsers = await _context.Users.CountAsync(),
				TotalStars = await _context.Stars.CountAsync(),
				TotalComments = await _context.Comments.CountAsync(),
				TotalLikes = await _context.Likes.CountAsync()
			};
			return Ok(stats);
		}

		// 2. API KIỂM DUYỆT (XÓA BÀI CỦA BẤT KỲ AI)
		[HttpDelete("force-delete-star/{id}")]
		public async Task<IActionResult> ForceDeleteStar(int id)
		{
			var star = await _context.Stars.FindAsync(id);
			if (star == null) return NotFound(new { message = "Không tìm thấy bài viết" });

			try
			{
				// 1. Dọn dẹp thông báo
				var notifications = _context.Notifications.Where(n => n.StarId == id).ToList();
				if (notifications.Any())
				{
					_context.Notifications.RemoveRange(notifications);
				}

				// 2. Dọn dẹp bình luận
				var comments = _context.Comments.Where(c => c.StarId == id).ToList();
				if (comments.Any())
				{
					_context.Comments.RemoveRange(comments);
				}

				// 3. Dọn dẹp lượt thích
				var likes = _context.Likes.Where(l => l.StarId == id).ToList();
				if (likes.Any())
				{
					_context.Likes.RemoveRange(likes);
				}

				// 4. DỌN DẸP BÀI ĐÃ LƯU (Thủ phạm thường giấu mặt ở đây)
				// Lưu ý: Nếu biến DbContext của bạn tên khác (vd: Saves), hãy đổi chữ SavedStars thành Saves
				var saves = _context.SavedStars.Where(s => s.StarId == id).ToList();
				if (saves.Any())
				{
					_context.SavedStars.RemoveRange(saves);
				}

				// 5. Sau khi đã dọn sạch, tiến hành xóa "cha"
				_context.Stars.Remove(star);

				// 6. Lưu thay đổi
				await _context.SaveChangesAsync();

				return Ok(new { message = "Đã tiêu hủy bài viết và các dữ liệu liên quan thành công" });
			}
			catch (Exception ex)
			{
				// Lấy nguyên nhân sâu xa nhất của SQL Server để dễ gỡ lỗi
				var realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
				return BadRequest(new { message = "Lỗi SQL: " + realError });
			}
		}
		// LẤY DANH SÁCH TOÀN BỘ BÀI VIẾT ĐỂ ADMIN ĐI TUẦN
		[HttpGet("all-stars")]
		public async Task<IActionResult> GetAllStarsForAdmin()
		{
			var stars = await _context.Stars
				.Include(s => s.User)
				.OrderByDescending(s => s.CreatedAt)
				.Select(s => new {
					id = s.Id,
					author = s.User.Username,
					content = s.Content,
					createdAt = s.CreatedAt,
					likeCount = s.LikeCount
				})
				.ToListAsync();
			return Ok(stars);
		}

		[HttpGet("export-stars")]
		public async Task<IActionResult> ExportStarsToExcel()
		{
			// 1. Lấy toàn bộ dữ liệu bài viết kèm tên tác giả
			var stars = await _context.Stars
				.Include(s => s.User)
				.OrderByDescending(s => s.CreatedAt)
				.ToListAsync();

			// 2. Tạo một file Excel ảo (Workbook)
			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Danh Sách Vì Sao");
				var currentRow = 1;

				// 3. Tạo Tiêu đề các cột (Header) và bôi đậm
				worksheet.Cell(currentRow, 1).Value = "ID Bài viết";
				worksheet.Cell(currentRow, 2).Value = "Tác giả";
				worksheet.Cell(currentRow, 3).Value = "Nội dung tần số";
				worksheet.Cell(currentRow, 4).Value = "Mã màu cảm xúc";
				worksheet.Cell(currentRow, 5).Value = "Ngày phóng sao";
				worksheet.Range("A1:E1").Style.Font.Bold = true;

				// 4. Đổ dữ liệu vào từng dòng
				foreach (var star in stars)
				{
					currentRow++;
					worksheet.Cell(currentRow, 1).Value = star.Id;
					worksheet.Cell(currentRow, 2).Value = star.User?.Username ?? "Vô danh";
					worksheet.Cell(currentRow, 3).Value = star.Content;
					worksheet.Cell(currentRow, 4).Value = star.SentimentColor;
					worksheet.Cell(currentRow, 5).Value = star.CreatedAt.ToString("dd/MM/yyyy HH:mm");
				}

				// Tự động căn chỉnh độ rộng cột cho đẹp
				worksheet.Columns().AdjustToContents();

				// 5. Đóng gói file và gửi về cho React tải xuống
				using (var stream = new MemoryStream())
				{
					workbook.SaveAs(stream);
					var content = stream.ToArray();

					return File(
						content,
						"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
						$"GalaxyVibes_Stars_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
					);
				}
			}
		}
	}
}