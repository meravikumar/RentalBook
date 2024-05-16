using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using RentalBook.Models.Authentication;

namespace RentalBook.Models.Models
{
	public class ShoppingCart
	{
		[Key]
		public int Id { get; set; }
		public int ProductId { get; set; }
		[ForeignKey("ProductId")]
		[ValidateNever]
		public Product Product { get; set; }

		[Range(1, 1000, ErrorMessage = "Please Enter range between 1 and 1000.")]
		public int Quantity { get; set; }
		public string UserId { get; set; }
		[ForeignKey("UserId")]
		[ValidateNever]
		public ApplicationUser ApplicationUser { get; set; }
		public double Price { get; set; }

		[NotMapped]
		public DiscountModel Discount { get; set; }

		public int RentDuration { get; set; } = 1;

	}
}
