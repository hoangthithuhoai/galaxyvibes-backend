using Galaxyvibes.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Galaxyvibes.API.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}

		// Đăng ký các bảng vào hệ thống
		public DbSet<User> Users { get; set; }
		public DbSet<Star> Stars { get; set; }
		public DbSet<Like> Likes { get; set; }
		public DbSet<Comment> Comments { get; set; }
		public DbSet<Follow> Follows { get; set; }
		public DbSet<SavedStar> SavedStars { get; set; }
		public DbSet<Notification> Notifications { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// 1. Quan hệ User - Star (1 người dùng có nhiều vì sao)
			modelBuilder.Entity<Star>()
				.HasOne(s => s.User)
				.WithMany(u => u.Stars)
				.HasForeignKey(s => s.UserId);

			// 2. Cấu hình quan hệ Follow (Tương tác giữa User - User)
			// Cấu hình cho người nhấn nút Theo dõi (Follower)
			modelBuilder.Entity<Follow>()
				.HasOne(f => f.Follower)
				.WithMany(u => u.Following)
				.HasForeignKey(f => f.FollowerId)
				.OnDelete(DeleteBehavior.Restrict); // Không cho xóa dây chuyền để tránh lỗi SQL

			// Cấu hình cho người được theo dõi (Following)
			modelBuilder.Entity<Follow>()
				.HasOne(f => f.Following)
				.WithMany(u => u.Followers)
				.HasForeignKey(f => f.FollowingId)
				.OnDelete(DeleteBehavior.Restrict);

			// 3. Quan hệ Like
			modelBuilder.Entity<Like>()
				.HasOne(l => l.Star)
				.WithMany(s => s.Likes)
				.HasForeignKey(l => l.StarId)
				.OnDelete(DeleteBehavior.Cascade); // Xóa bài viết thì các Like bay theo

			modelBuilder.Entity<Like>()
				.HasOne(l => l.User)
				.WithMany(u => u.Likes)
				.HasForeignKey(l => l.UserId)
				.OnDelete(DeleteBehavior.Restrict); // GIẢI QUYẾT LỖI: Không xóa dây chuyền từ User

			// 4. Quan hệ Comment
			modelBuilder.Entity<Comment>()
				.HasOne(c => c.Star)
				.WithMany(s => s.Comments)
				.HasForeignKey(c => c.StarId)
				.OnDelete(DeleteBehavior.Cascade); // Xóa bài viết thì các Comment bay theo

			modelBuilder.Entity<Comment>()
				.HasOne(c => c.User)
				.WithMany(u => u.Comments)
				.HasForeignKey(c => c.UserId)
				.OnDelete(DeleteBehavior.Restrict); // GIẢI QUYẾT LỖI: Không xóa dây chuyền từ User

			// 5. Quan hệ Lưu bài viết (SavedStar)
			modelBuilder.Entity<SavedStar>()
				.HasOne(ss => ss.Star)
				.WithMany() // Một Star có thể được nhiều người lưu, nhưng không cần list ngược lại trong Star
				.HasForeignKey(ss => ss.StarId)
				.OnDelete(DeleteBehavior.Cascade); // Xóa bài thì danh sách lưu cũng bay màu

			modelBuilder.Entity<SavedStar>()
				.HasOne(ss => ss.User)
				.WithMany(u => u.SavedStars)
				.HasForeignKey(ss => ss.UserId)
				.OnDelete(DeleteBehavior.Restrict); // Không xóa dây chuyền từ User

			// 6. Quan hệ Thông báo (Notification)
			modelBuilder.Entity<Notification>()
				.HasOne(n => n.User)
				.WithMany()
				.HasForeignKey(n => n.UserId)
				.OnDelete(DeleteBehavior.Cascade); // Xóa User thì xóa thông báo của họ

			modelBuilder.Entity<Notification>()
				.HasOne(n => n.Actor)
				.WithMany()
				.HasForeignKey(n => n.ActorId)
				.OnDelete(DeleteBehavior.Restrict); // TRÁNH LỖI: Không xóa dây chuyền từ người tạo hành động
		}
	}
}