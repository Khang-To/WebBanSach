using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class CartItem
    {
        public int ID { get; set; }

        public int UserId { get; set; }     // không cần [Required] nếu User đang đăng nhập
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        // Navigation: nullable
        public virtual AppUser? User { get; set; }
        public virtual Product? Product { get; set; }
    }
}
