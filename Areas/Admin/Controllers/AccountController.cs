using Microsoft.AspNetCore.Mvc;
using System;
using WebBanSach.Filters;
using WebBanSach.Models;

namespace WebBanSach.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.AppUsers.FirstOrDefault(u =>
                u.UserName == username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "Tài khoản hoặc mật khẩu không đúng.";
                return View();
            }

            if (user.UserType != UserType.Admin)
            {
                ViewBag.Error = "Bạn không có quyền truy cập khu vực Admin.";
                return View();
            }

            // DÙNG KEY RIÊNG CHO ADMIN – KHÔNG DÍNH VỚI KHÁCH
            HttpContext.Session.SetInt32("Admin_UserId", user.ID);
            HttpContext.Session.SetString("Admin_UserType", "Admin");
            HttpContext.Session.SetString("Admin_Name", user.FullName ?? "Admin");

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        public IActionResult Logout()
        {
            // XÓA CHỈ SESSION CỦA ADMIN
            HttpContext.Session.Remove("Admin_UserId");
            HttpContext.Session.Remove("Admin_UserType");
            HttpContext.Session.Remove("Admin_Name");
            return RedirectToAction("Login");
        }

        // Các action khác (Profile, EditProfile, ChangePassword) giữ nguyên
        // Chỉ cần sửa chỗ lấy Session: dùng "Admin_UserId" thay vì "UserId"
        [AdminAuthorize]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("Admin_UserId")!.Value;
            var admin = _context.AppUsers.Find(userId);
            return admin == null ? NotFound() : View(admin);
        }

        // POST: Sửa thông tin
        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(AppUser model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var admin = _context.AppUsers.Find(userId);

            if (admin == null) return NotFound();

            // Chỉ cho sửa những field này (không cho sửa UserName, UserType, IsActive ở đây)
            admin.FullName = model.FullName;
            admin.Email = model.Email;
            admin.PhoneNumber = model.PhoneNumber;
            admin.Address = model.Address;

            try
            {
                _context.Update(admin);
                _context.SaveChanges();
                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Profile));
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu.");
                return View(model);
            }
        }

        // GET: Đổi mật khẩu
        [AdminAuthorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Đổi mật khẩu
        [AdminAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Mật khẩu mới phải ít nhất 6 ký tự.";
                return View();
            }

            var userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var admin = _context.AppUsers.Find(userId);

            if (admin == null) return NotFound();

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, admin.PasswordHash))
            {
                ViewBag.Error = "Mật khẩu cũ không đúng.";
                return View();
            }

            // Cập nhật mật khẩu mới
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Update(admin);
            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Login");
        }
    }
}
