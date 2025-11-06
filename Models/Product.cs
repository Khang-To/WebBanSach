using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanSach.Models
{
    public class Product
    {
        public int ID { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Tên sách")]
        public string Name { get; set; } = null!;

        [Display(Name = "Mô tả")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, Range(0, double.MaxValue)]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Range(0, 100)]
        [Display(Name = "Giảm giá")]
        public decimal? DiscountPercent { get; set; }

        [Required, Range(0, int.MaxValue)]
        [Display(Name = "Số lượng tồn")]
        public int Stock { get; set; }

        [Display(Name = "Ảnh sản phẩm")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Năm xuất bản")]
        public int? PublishYear { get; set; }

        [Display(Name = "Tổng số trang")]
        public int? TotalPage { get; set; }

        [Display(Name = "Thể loại")]
        public int CategoryId { get; set; }

        [Display(Name = "Tác giả")]
        public int AuthorId { get; set; }

        [Display(Name = "Nhà xuất bản")]
        public int PublisherId { get; set; }

        // tham chiếu
        public virtual ProductCategory Category { get; set; } = null!;
        public virtual Author Author { get; set; } = null!;
        public virtual Publisher Publisher { get; set; } = null!;
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Helper (không lưu DB)
        [NotMapped]
        [Display(Name = "Giá sau giảm")]
        public decimal FinalPrice => Price * (1 - (DiscountPercent ?? 0) / 100);    // giá sau khi giảm
    }
}
