namespace Galaxyvibes.API.Models
{
	public class SavedStar
	{
		public int Id { get; set; }
		public int UserId { get; set; }
		public int StarId { get; set; }
		public DateTime SavedAt { get; set; } = DateTime.UtcNow;

		public User? User { get; set; }
		public Star? Star { get; set; }
	}
}