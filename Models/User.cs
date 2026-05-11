using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Galaxyvibes.API.Models
{
	public class User
	{
		[Key]
		public int Id { get; set; }

		// ==========================================
		// BỌC THÉP TÀI KHOẢN NGƯỜI DÙNG
		// ==========================================
		[Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
		[StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
		public string Username { get; set; } = string.Empty;

		[Required(ErrorMessage = "Email là bắt buộc.")]
		[EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ.")]
		[StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
		[StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
		public string Password { get; set; } = string.Empty;

		[MaxLength]
		public string? AvatarUrl { get; set; }

		[Required]
		[StringLength(20)]
		public string Role { get; set; } = "User";

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// ==========================================
		// CÁC MỐI QUAN HỆ 
		// ==========================================
		// Một người dùng có thể tạo ra nhiều vì sao (bài viết)
		public ICollection<Star> Stars { get; set; } = new List<Star>();

		public ICollection<Like> Likes { get; set; } = new List<Like>();
		public ICollection<Comment> Comments { get; set; } = new List<Comment>();

		public ICollection<Follow> Followers { get; set; } = new List<Follow>();
		public ICollection<Follow> Following { get; set; } = new List<Follow>();

		public ICollection<SavedStar> SavedStars { get; set; } = new List<SavedStar>();
	}
}