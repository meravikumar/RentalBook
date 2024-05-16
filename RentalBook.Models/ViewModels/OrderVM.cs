using RentalBook.Models.Authentication;
using RentalBook.Models.Models;

namespace RentalBook.Models.ViewModels
{
	public class OrderVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public double OrderTotal { get; set; }
        public IEnumerable<OrderDetail> OrderDetails { get; set; }
        public OrderHeader OrderHeader { get; set; }
        public Product Product { get; set; }
        public ShoppingCart Cart { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}
