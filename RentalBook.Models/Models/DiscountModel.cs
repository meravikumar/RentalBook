using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RentalBook.Models.Models
{
	public class DiscountModel
	{
		[Key]
		public int Id { get; set; }
		[Required]
		[ForeignKey("ProductId")]
		public int ProductId { get; set; }
		public string DiscountType { get; set; }
		public double Discount { get; set; }
		public DateTime ValidFrom { get; set; } = DateTime.Now.Date;
		public DateTime ValidTo { get; set; } = DateTime.Now.Date;

	}
}
