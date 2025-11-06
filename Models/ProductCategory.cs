using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class ProductCategory
    {
        public int ID { get; set; }                   // Khóa chính

        [Required, StringLength(100)]
        [Display(Name = "Tên thể loại")]
        public string Name { get; set; } = null!;

        [StringLength(300)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // tham chiếu
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
