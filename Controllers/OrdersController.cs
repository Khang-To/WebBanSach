using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanSach.Models;
using System.Linq;
using System.Threading.Tasks;

namespace WebBanSach.Controllers
{
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy UserId từ Session (dùng chung toàn controller)
        private int? CurrentUserId => HttpContext.Session.GetInt32("Customer_UserId");
        private bool IsLoggedIn => CurrentUserId.HasValue;

        // Xem chi tiết danh sách đơn hàng của mình có mode để sử dụng làm trang thanh toán hiển thị các đơn hàng đã được confirm
        public async Task<IActionResult> Index(string mode = "all", int page = 1)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var userId = CurrentUserId.Value;
            int pageSize = 5; // số đơn hàng mỗi trang (tùy chỉnh thoải mái: 5, 10, 20...)

            IQueryable<Order> query = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Publisher)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Where(o => o.UserId == userId);

            // Lọc theo mode (tất cả hoặc chỉ đơn chờ thanh toán)
            if (mode == "payment")
            {
                query = query.Where(o => o.Status == OrderStatus.Confirmed);
                ViewBag.Title = "Các đơn hàng chờ thanh toán";
                ViewBag.ShowPayButtonOnly = true;
            }
            else
            {
                ViewBag.Title = "Đơn hàng của bạn";
                ViewBag.ShowPayButtonOnly = false;
            }

            // === PHÂN TRANG - CHỈ THÊM 3 DÒNG NÀY ===
            var totalItems = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Truyền dữ liệu phân trang ra View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Mode = mode;

            if (!orders.Any() && page == 1)
            {
                ViewBag.Message = mode == "payment"
                    ? "Bạn không có đơn hàng nào chờ thanh toán."
                    : "Bạn chưa có đơn hàng nào.";
            }

            return View(orders);
        }

        // GET: Orders/Create → Trang xác nhận đơn hàng
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.ID == CurrentUserId.Value);

            // BẮT BUỘC phải có 3 thông tin này mới được đặt hàng
            if (string.IsNullOrWhiteSpace(user.FullName) ||
                string.IsNullOrWhiteSpace(user.Address) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                TempData["Warning"] = "Vui lòng cập nhật đầy đủ thông tin cá nhân trước khi đặt hàng!";
                return RedirectToAction("EditProfile", "Account"); // chuyển thẳng sang trang sửa hồ sơ
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == CurrentUserId.Value)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("ViewCart", "Cart");
            }

            ViewBag.CartItems = cartItems;
            ViewBag.TotalAmount = cartItems.Sum(c => c.Quantity * c.Product.FinalPrice);
            ViewBag.CurrentUser = user;

            return View();
        }

        // POST: Xác nhận đặt hàng (từ trang xác nhận)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string note)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var userId = CurrentUserId.Value;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("ViewCart", "Cart");

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // để có order.ID

            // Tạo chi tiết đơn hàng + giảm tồn kho
            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.ID,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.FinalPrice
                });

                item.Product.Stock -= item.Quantity;
            }

            // Xóa giỏ hàng
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", new { id = order.ID });
        }

        // POST: Khách hủy đơn hàng và cập nhật lại tồn kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            if (!IsLoggedIn)
                return RedirectToAction("Login", "Account");

            var userId = CurrentUserId.Value;

            // Lấy đơn hàng kèm chi tiết + sản phẩm
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.ID == orderId && o.UserId == userId);

            if (order == null)
                return NotFound();

            // CHỈ CHO PHÉP HỦY KHI ĐƠN CÒN PENDING
            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ xác nhận.";
                return RedirectToAction("Index");
            }

            // Dùng transaction để đảm bảo cộng kho và đổi trạng thái cùng thành công hoặc cùng thất bại
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Cộng lại tồn kho cho từng sản phẩm
                foreach (var item in order.OrderDetails)
                {
                    item.Product.Stock += item.Quantity;
                }

                // 2. Đánh dấu đơn hàng là Cancelled
                order.Status = OrderStatus.Cancelled;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Đã hủy đơn hàng #{order.ID} thành công. Tồn kho đã được cập nhật lại.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Có lỗi xảy ra khi hủy đơn hàng. Vui lòng thử lại!";
                return RedirectToAction("Index");
            }
        }

        // Trang đặt hàng thành công
        public async Task<IActionResult> OrderSuccess(int id)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.ID == id && o.UserId == CurrentUserId.Value);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: thanh toán đơn hàng cập nhật trạng thái đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayOrder(int orderId)
        {
            if (!IsLoggedIn) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.ID == orderId && o.UserId == CurrentUserId.Value);

            if (order == null) return NotFound();

            // CHỈ đổi trạng thái, KHÔNG trừ kho
            order.Status = OrderStatus.Paid;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Thanh toán đơn hàng #{order.ID} thành công!";
            return RedirectToAction("PaymentSuccess", new { id = order.ID });
        }

        // GET: hiển thị trang thanh toán thành công
        public async Task<IActionResult> PaymentSuccess(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.ID == id && o.UserId == CurrentUserId.Value);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}