using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    // Enum trạng thái đơn hàng
    public enum OrderStatus
    {
        [Display(Name = "Chờ xử lý")]
        Pending = 0,

        [Display(Name = "Đã xác nhận")]
        Confirmed = 1,

        [Display(Name = "Đã thanh toán")]
        Paid = 2,

        [Display(Name = "Đã hủy")]
        Cancelled = 3
    }

    public class Order
    {
        public int ID { get; set; }

        [Display(Name = "Khách hàng")]
        public int UserId { get; set; }

        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Trạng thái đơn hàng")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;  //mặc định là đang chờ xử lý

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Navigation: KHÔNG [Required], phải nullable
        public virtual AppUser? User { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
