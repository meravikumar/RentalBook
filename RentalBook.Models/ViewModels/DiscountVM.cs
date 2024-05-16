namespace RentalBook.Models.ViewModels
{
    public class DiscountVM
    {
        public int ProductId { get; set; }
        public double Discount { get; set; }
        public string DiscountType { get; set; }
        public DateTime ValidFrom { get; set; } = DateTime.Now;
        public DateTime ValidTo { get; set; } = DateTime.Now;
        public double Price { get; set; }

    }
}
