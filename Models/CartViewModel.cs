using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanSach.Models
{
    public class CartViewModel
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }

        [NotMapped]
        public decimal Subtotal => Quantity * Product.FinalPrice;
    }
}
