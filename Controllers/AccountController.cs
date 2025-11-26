using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebBanSach.Filters;
using WebBanSach.Models;
namespace WebBanSach.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị form đăng ký
        public IActionResult Register()
        {
            return View();
        }

        // POST: Đăng ký tài khoản của khách
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra username trùng → gắn lỗi vào field UserName
            if (await _context.AppUsers.AnyAsync(x => x.UserName == model.UserName))
            {
                ModelState.AddModelError(nameof(model.UserName), "Tên tài khoản đã tồn tại");
                return View(model);
            }

            // Kiểm tra email trùng → gắn lỗi vào field Email
            if (await _context.AppUsers.AnyAsync(x => x.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng");
                return View(model);
            }

            // tạo AppUser để lưu DB
            var user = new AppUser
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                UserType = UserType.Customer,
                IsActive = true
            };
            _context.Add(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Login");
        }

        // GET: Hiển thị form đăng nhập
        public IActionResult Login()
        {
            return View();
        }

        // POST: Đăng nhập lưu session ID và tên của người dùng
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(x => x.UserName == model.UserName);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ cửa hàng để được hỗ trợ.");
                return View(model);
            }

            // DÙNG KEY RIÊNG CHO KHÁCH – KHÔNG DÍNH VỚI ADMIN
            HttpContext.Session.SetInt32("Customer_UserId", user.ID);
            HttpContext.Session.SetString("Customer_UserType", "Customer");
            HttpContext.Session.SetString("Customer_Name", user.FullName);

            try
            {
                await MergeCartSessionToDatabase(user.ID);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Merge cart error: {ex.Message}");
                TempData["Error"] = "Có lỗi khi cập nhật giỏ hàng. Vui lòng thử lại.";
            }

            return RedirectToAction("Index", "Home");
        }

        // Hàm merge giỏ hàng session vào database
        private async Task MergeCartSessionToDatabase(int userId)
        {
            var session = HttpContext.Session;
            string? jsoncart = session.GetString("shopcart");
            if (string.IsNullOrEmpty(jsoncart)) return; // Không có cart, skip

            // Settings để ignore circular reference (fix lỗi deserialize)
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Ignore loops từ EF navigation
                NullValueHandling = NullValueHandling.Ignore // Skip null properties
            };

            List<CartViewModel>? sessionCart = null;

            try
            {
                sessionCart = JsonConvert.DeserializeObject<List<CartViewModel>>(jsoncart, settings);
            }
            catch (JsonException ex)
            {
                // Nếu deserialize fail, log và clear session cart hỏng
                System.Diagnostics.Debug.WriteLine($"Deserialize cart error: {ex.Message}");
                session.Remove("shopcart"); // Clear để tránh loop lỗi
                throw new InvalidOperationException("Giỏ hàng session bị hỏng, đã clear.");
            }

            if (sessionCart == null || !sessionCart.Any())
            {
                session.Remove("shopcart");
                return;
            }

            // Lấy giỏ DB hiện có
            var dbCart = await _context.CartItems.Where(x => x.UserId == userId).ToListAsync();

            // Load products từ DB để check stock (không dùng item.Product từ session, vì có thể outdated)
            var productIds = sessionCart.Select(i => i.Product.ID).Distinct().ToList();

            var productsDict = await _context.Products.Where(p => productIds.Contains(p.ID)).ToDictionaryAsync(p => p.ID);    

            foreach (var item in sessionCart)
            {
                if (!productsDict.TryGetValue(item.Product.ID, out var product)) continue; // Skip nếu product không tồn tại

                var existing = dbCart.FirstOrDefault(x => x.ProductId == item.Product.ID);
                int newQuantity = item.Quantity;

                if (existing != null)
                {
                    newQuantity += existing.Quantity;
                }

                // Check stock
                if (newQuantity > product.Stock)
                {
                    TempData["Warning"] = $"Sản phẩm '{product.Name}' chỉ còn {product.Stock} trong kho, đã điều chỉnh số lượng.";
                    newQuantity = Math.Min(newQuantity, product.Stock); // Adjust thay vì skip
                }

                if (existing == null)
                {
                    _context.CartItems.Add(new CartItem
                    {
                        UserId = userId,
                        ProductId = item.Product.ID,
                        Quantity = newQuantity
                    });
                }
                else
                {
                    existing.Quantity = newQuantity;
                    _context.CartItems.Update(existing);
                }
            }
            await _context.SaveChangesAsync();
            session.Remove("shopcart"); // Clear session sau merge
        }

        // GET: Đăng xuất xóa session UserId
        public IActionResult Logout()
        {
            // XÓA CHỈ SESSION CỦA KHÁCH
            HttpContext.Session.Remove("Customer_UserId");
            HttpContext.Session.Remove("Customer_UserType");
            HttpContext.Session.Remove("Customer_Name");
            HttpContext.Session.Remove("shopcart"); // xóa giỏ hàng session nếu cần
            return RedirectToAction("Index", "Home");
        }

        [CustomerAuthorize]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("Customer_UserId")!.Value;
            var customer = _context.AppUsers.Find(userId);
            return customer == null ? NotFound() : View(customer);
        }

        // GET: Account/EditProfile
        [CustomerAuthorize]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("Customer_UserId")!.Value;
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.ID == userId);

            if (user == null) return NotFound();

            // Dùng ViewModel để dễ validate (tùy chọn, hoặc giữ nguyên AppUser cũng được)
            var model = new AppUser
            {
                ID = user.ID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address ?? ""
            };

            // Nếu đang từ Orders/Create bị chặn → hiện thông báo
            if (TempData["Warning"] != null)
            {
                ViewBag.Warning = TempData["Warning"];
            }

            return View(model);
        }

        // POST: chỉnh sửa thông tin nếu thiếu bắt buộc phải nhập đủ sđt và địa chỉ
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomerAuthorize]
        public async Task<IActionResult> EditProfile(AppUser model)
        {
            try
            {
                if (model.ID <= 0)
                {
                    ModelState.AddModelError("", "ID không hợp lệ!");
                    return View(model);
                }

                var user = await _context.AppUsers.FindAsync(model.ID);
                if (user == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy user trong DB!");
                    return View(model);
                }

                // Cập nhật các thông tin không cần check
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;

                // --- Kiểm tra email ---
                if (user.Email != model.Email) // chỉ kiểm tra khi email được thay đổi
                {
                    bool emailExists = await _context.AppUsers
                        .AnyAsync(x => x.Email == model.Email && x.ID != model.ID);

                    if (emailExists)
                    {
                        ModelState.AddModelError("", "Email đã tồn tại");
                        return View(model);
                    }

                    user.Email = model.Email; // cập nhật email mới
                }

                _context.AppUsers.Update(user);
                await _context.SaveChangesAsync();

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(model);
            }
        }

        // GET: Đổi mật khẩu
        [CustomerAuthorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Đổi mật khẩu
        [CustomerAuthorize]
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
            var customer = _context.AppUsers.Find(userId);

            if (customer == null) return NotFound();

            // Kiểm tra mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, customer.PasswordHash))
            {
                ViewBag.Error = "Mật khẩu cũ không đúng.";
                return View();
            }

            // Cập nhật mật khẩu mới
            customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Update(customer);
            _context.SaveChanges();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Login");
        }
    }
}