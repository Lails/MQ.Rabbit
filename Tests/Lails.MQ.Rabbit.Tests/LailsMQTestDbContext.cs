using Microsoft.EntityFrameworkCore;
using Lails.MQ.Rabbit.Tests.Model;

namespace Lails.MQ.Rabbit.Tests
{
	public class LailsMQTestDbContext : DbContext
	{
		public LailsMQTestDbContext(DbContextOptions options) : base(options) { }

		public DbSet<Point> Points { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Point>().HasKey(r => r.Id);

		}
	}
}
