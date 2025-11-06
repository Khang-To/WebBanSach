using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class ProductReview
    {
        public int ID { get; set; }

        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [Display(Name = "Người dùng")]
        public int UserId { get; set; }

        [Range(1, 5)]
        [Display(Name = "Đánh giá")]
        public int Rating { get; set; }

        [StringLength(500)]
        [Display(Name = "Bình luận")]
        public string? Comment { get; set; }

        [Display(Name = "Ngày đánh giá")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // tham chiếu
        public virtual Product Product { get; set; } = null!;
        public virtual AppUser User { get; set; } = null!;
    }
}
