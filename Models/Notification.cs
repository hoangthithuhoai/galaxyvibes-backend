namespace Galaxyvibes.API.Models
{
	public class Notification
	{
		public int Id { get; set; }
		public int UserId { get; set; } // Người NHẬN thông báo
		public int ActorId { get; set; } // Người GÂY RA hành động (người like/comment)
		public string Type { get; set; } = string.Empty; // "LIKE", "COMMENT", "FOLLOW"
		public int? StarId { get; set; } // ID bài viết liên quan (nếu có)
		public string Message { get; set; } = string.Empty;
		public bool IsRead { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Liên kết dữ liệu
		public User? User { get; set; }
		public User? Actor { get; set; }
		public Star? Star { get; set; }
	}
}