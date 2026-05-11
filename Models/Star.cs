using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Galaxyvibes.API.Models
{
	public class Star
	{
		[Key]
		public int Id { get; set; }

		public int LikeCount { get; set; }
		public int CommentCount { get; set; }
		public string? ImageUrl { get; set; }

		// ==========================================
		// THÊM VALIDATE: Bọc thép nội dung vì sao
		// ==========================================
		[Required(ErrorMessage = "Nội dung vì sao không được để trống.")]
		[StringLength(500, MinimumLength = 1, ErrorMessage = "Nội dung phải từ 1 đến 500 ký tự.")]
		public string Content { get; set; } = string.Empty;

		// Bọc thép mã màu sắc
		[Required(ErrorMessage = "Mã màu sắc cảm xúc là bắt buộc.")]
		[StringLength(20, ErrorMessage = "Mã màu không hợp lệ.")]
		public string SentimentColor { get; set; } = "#FFFFFF";

		// Kích thước của sao (dựa trên độ dài văn bản)
		public int Size { get; set; } = 1;

		// Tọa độ 3D trên không gian dải ngân hà
		public float PositionX { get; set; }
		public float PositionY { get; set; }
		public float PositionZ { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// ==========================================
		// KHÓA NGOẠI RÕ RÀNG
		// ==========================================
		[Required]
		public int UserId { get; set; }

		[ForeignKey("UserId")]
		public User? User { get; set; }

		// Các danh sách liên kết
		public ICollection<Like> Likes { get; set; } = new List<Like>();
		public ICollection<Comment> Comments { get; set; } = new List<Comment>();
	}
}