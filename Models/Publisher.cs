using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class Publisher
    {
        public int ID { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tên nhà xuất bản")]
        public string Name { get; set; } = null!;

        //Tham chiếu
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
