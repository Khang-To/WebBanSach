using System.ComponentModel.DataAnnotations;

namespace WebBanSach.Models
{
    public class RegisterViewModel
    {
        [Required, StringLength(50)]
        public string UserName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }

        [Required, Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; }
    }

}
