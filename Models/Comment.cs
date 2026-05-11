using Galaxyvibes.API.Models;

public class Comment
{
	public int Id { get; set; }
	public string Content { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; } = DateTime.Now;
	public int UserId { get; set; }
	public int StarId { get; set; }
	public User? User { get; set; }
	public Star? Star { get; set; }
}