using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalBook.Models.Authentication;
using RentalBook.Models.Models;

namespace RentalBook.DataAccess.Data
{
	public class AppDbContext : IdentityDbContext<ApplicationUser>
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{

		}
		public DbSet<Product> Products { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<DiscountModel> Discounts { get; set; }
		public DbSet<ShoppingCart> ShoppingCarts { get; set; }
		public DbSet<OrderHeader> OrderHeaders { get; set; }
		public DbSet<OrderDetail> OrderDetails { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			SeedRoles(builder);
		}
		private static void SeedRoles(ModelBuilder builder)
		{
			builder.Entity<IdentityRole>().HasData
				(
				new IdentityRole() { Name = "SuperAdmin", ConcurrencyStamp = "1", NormalizedName = "SuperAdmin" },
				new IdentityRole() { Name = "Admin", ConcurrencyStamp = "2", NormalizedName = "Admin" },
				new IdentityRole() { Name = "Dealer", ConcurrencyStamp = "3", NormalizedName = "Dealer" },
				new IdentityRole() { Name = "User", ConcurrencyStamp = "4", NormalizedName = "User" }

				);
		}


		//https://www.techstrikers.com/Articles/custom-user-registration-and-login-page-with-entity-framework.php
		//https://www.freecodespot.com/blog/asp-net-core-identity/#:~:text=Open%20AccountController.,Registration%20of%20our%20user%20identity.
	}
}
