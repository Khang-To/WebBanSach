using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace WebBanSach.Filters
{
    /// <summary>
    /// Filter chỉ cho phép KHÁCH HÀNG đã đăng nhập (UserType = Customer)
    /// Dùng cho: Thanh toán, hồ sơ cá nhân khách, xem đơn hàng cá nhân, đổi mật khẩu khách...
    /// </summary>
    public class CustomerAuthorizeAttribute : ActionFilterAttribute
    {
        // Có thể override khi dùng: [CustomerAuthorize(LoginController = "Home", LoginAction = "Index")]
        public string LoginController { get; set; } = "Account";
        public string LoginAction { get; set; } = "Login";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // ĐỌC KEY RIÊNG CỦA KHÁCH
            var userId = session.GetInt32("Customer_UserId");
            var userType = session.GetString("Customer_UserType");

            if (!userId.HasValue || userType != "Customer")
            {
                bool isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                              context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json");

                if (isAjax)
                {
                    context.Result = new JsonResult(new { success = false, message = "Vui lòng đăng nhập để tiếp tục." })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                }
                else
                {
                    var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                }
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}