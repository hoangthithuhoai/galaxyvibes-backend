using Galaxyvibes.API.Models;

public class Like
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public int StarId { get; set; }
	public User? User { get; set; }
	public Star? Star { get; set; }
}