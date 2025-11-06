using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class CartItem
    {
        public int ID { get; set; }

        [Display(Name = "Người dùng")]
        public int UserId { get; set; }

        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        public virtual AppUser User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
