using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RentalBook.Models.Models
{
	public class Product
	{
		[Key]
		public int Id { get; set; }
		[Required]
		public string Title { get; set; }
		public string Description { get; set; }
		public string ISBN { get; set; }
		public string Author { get; set; }
		public string Publication { get; set; }
		public string Language { get; set; }
		public double Price { get; set; }
		public double? DiscountAmt { get; set; }
		public bool IsActive { get; set; }
		[Range(1, int.MaxValue, ErrorMessage = "Please enter a value {1} or bigger than {1}")]
		public int Quantity { get; set; }

		[ForeignKey("CategoryId")]
		public int CategoryId { get; set; }

		[ForeignKey("UserId")]
		public string UserId { get; set; }
		public string Renter { get; set; }
		[ValidateNever]
		public string ImageUrl { get; set; }
		public DateTime CreatedOn { get; set; } = DateTime.Now;

		public bool IsOrdered { get; set; }

	}
}
