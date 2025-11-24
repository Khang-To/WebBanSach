using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
        public IActionResult Index()
        {
            return View();
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
            // kiểm tra username trùng
            if (_context.AppUsers.Any(x => x.UserName == model.UserName))
            {
                ModelState.AddModelError("", "Tên tài khoản đã tồn tại");
                return View(model);
            }
            // kiểm tra email trùng
            if (_context.AppUsers.Any(x => x.Email == model.Email))
            {
                ModelState.AddModelError("", "Email đã tồn tại");
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
            if (!ModelState.IsValid)
                return View(model);
            var user = await _context.AppUsers
            .FirstOrDefaultAsync(x => x.UserName == model.UserName);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu");
                return View(model);
            }
            // lưu vào session
            HttpContext.Session.SetInt32("UserId", user.ID);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("UserType", user.UserType.ToString());
            // Merge cart (sẽ fix lỗi deserialize)
            try
            {
                await MergeCartSessionToDatabase(user.ID);
                TempData["Success"] = "Đăng nhập thành công! Giỏ hàng đã được cập nhật.";
            }
            catch (Exception ex)
            {
                // Log lỗi (xem console hoặc Application Insights)
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
            HttpContext.Session.Remove("UserId");
            return RedirectToAction("Index", "Home");
        }
    }
}