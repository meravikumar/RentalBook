using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RentalBook.Models.Models
{
	public class OrderDetail
	{
		[Key]
		public int Id { get; set; }
		[Required]
		public int OrderHeaderId { get; set; }
		[ForeignKey("OrderHeaderId")]
		[ValidateNever]
		public OrderHeader OrderHeader { get; set; }


		[Required]
		public int ProductId { get; set; }
		[ForeignKey("ProductId")]
		[ValidateNever]
		public Product Product { get; set; }

		public int Quantity { get; set; }
		public double Price { get; set; }

		public int RentDuration { get; set; } = 1; 
    }
}
