using Galaxyvibes.API.Models;

public class Follow
{
	public int Id { get; set; }
	public int FollowerId { get; set; } // Người nhấn nút Theo dõi
	public int FollowingId { get; set; } // Người được theo dõi
	public User? Follower { get; set; }
	public User? Following { get; set; }
}