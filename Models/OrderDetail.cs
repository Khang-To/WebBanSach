using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class OrderDetail
    {
        public int ID { get; set; }

        [Display(Name = "Đơn hàng")]
        public int OrderId { get; set; }

        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Đơn giá")]
        public decimal UnitPrice { get; set; }

        // Navigation
        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
