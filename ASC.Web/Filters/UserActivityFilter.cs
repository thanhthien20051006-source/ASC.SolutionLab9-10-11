using ASC.Utilities;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ASC.Web.Filters
{
    public class UserActivityFilter : IActionFilter
    {
        private readonly ILogger<UserActivityFilter> _logger;

        public UserActivityFilter(ILogger<UserActivityFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User?.Identity?.IsAuthenticated == true
                ? context.HttpContext.User.GetCurrentUserDetails().Email
                : "Anonymous";

            _logger.LogInformation(
                "UserActivity: {User} -> {Method} {Path}",
                user,
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                var user = context.HttpContext.User?.Identity?.IsAuthenticated == true
                    ? context.HttpContext.User.GetCurrentUserDetails().Email
                    : "Anonymous";

                _logger.LogError(
                    context.Exception,
                    "UserActivityError: {User} -> {Method} {Path}",
                    user,
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path);
            }
        }
    }
}
