using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class Author
    {
        public int ID { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tên tác giả")]
        public string Name { get; set; } = null!;

        [Display(Name = "Giới thiệu")]
        [StringLength(500)]
        public string? Description { get; set; }

        //Tham chiếu
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
