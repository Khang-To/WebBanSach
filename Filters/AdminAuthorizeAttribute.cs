using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebBanSach.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public string LoginController { get; set; } = "Account";
        public string LoginAction { get; set; } = "Login";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Lấy giá trị session UserType (lưu "Admin" khi đăng nhập)
            var userType = context.HttpContext.Session.GetString("UserType");

            // Nếu chưa đăng nhập hoặc không phải Admin
            if (string.IsNullOrEmpty(userType) || userType != "Admin")
            {
                // Nếu request là AJAX (fetch/xhr) trả về 401 JSON, tránh redirect html
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    || context.HttpContext.Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    context.Result = new JsonResult(new { message = "Unauthorized" })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                }
                else
                {
                    // Chuyển hướng về trang Login
                    context.Result = new RedirectToActionResult(LoginAction, LoginController, new { area = "Admin" });
                }

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
