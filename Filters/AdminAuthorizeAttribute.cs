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
            // ĐỌC KEY RIÊNG CỦA ADMIN
            var userType = context.HttpContext.Session.GetString("Admin_UserType");

            if (string.IsNullOrEmpty(userType) || userType != "Admin")
            {
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = 401 };
                }
                else
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Admin" });
                }
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
