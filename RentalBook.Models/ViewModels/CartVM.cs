using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using RentalBook.Models.Models;

namespace RentalBook.Models.ViewModels
{
	public class CartVM
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
        public int Id { get; set; }

        public string PhoneNumber { get; set; }
        public string Area { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string UserName { get; set; }
        public IEnumerable<ShoppingCart> CartItem { get; set; }
        public OrderHeader OrderHeader { get; set; }
        public Product Product { get; set; }
        public ShoppingCart Cart { get; set; }

		public int RentDuration { get; set; } = 1;
	}
}

