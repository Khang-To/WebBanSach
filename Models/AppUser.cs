using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    // enum phân loại người dùng
    public enum UserType
    {
        [Display(Name = "Khách hàng")]
        Customer = 0,

        [Display(Name = "Quản trị viên")]
        Admin = 1
    }

    public class AppUser
    {
        public int ID { get; set; }                         //Khóa chính

        [Required, StringLength(50)]
        [Display(Name = "Tài khoản")]
        public string UserName { get; set; } = null!;

        [Required]
        [Display(Name = "Mật khẩu")]
        public string PasswordHash { get; set; } = null!;

        [Required, StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = null!;

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Phone]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Loại người dùng")]
        public UserType UserType { get; set; } = UserType.Customer;

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        // Tham chiếu
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    
    }
}
