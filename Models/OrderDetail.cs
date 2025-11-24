using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class OrderDetail
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Đơn hàng")]
        public int OrderId { get; set; }

        [Required]
        [Display(Name = "Sách")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }   // số lượng mua

        [Range(0, double.MaxValue)]
        [Display(Name = "Đơn giá")]
        public decimal UnitPrice { get; set; }

        // Navigation: nullable, không [Required]
        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
    }
}
