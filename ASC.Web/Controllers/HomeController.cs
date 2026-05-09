using ASC.Web.Configuration;
using ASC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using ASC.Utilities;
namespace ASC.Web.Controllers
{
    public class HomeController : AnonymousController
    {
        private readonly IOptions<ApplicationSettings> _settings;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IOptions<ApplicationSettings> settings, ILogger<HomeController> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public IActionResult Index()
        {
            //// Set Session
            HttpContext.Session.SetSession("Test", _settings.Value);
            //// Get Session
            var settings = HttpContext.Session.GetSession<ApplicationSettings>("Test");
            //// Usage of IOptions
            ViewBag.Title = _settings.Value.ApplicationTitle;

            ////Test fail test case
            //ViewData.Model = "Test";
            //throw new Exception("Login Fail!!!");
            return View();
        }
        public IActionResult Dashboard()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Unhandled exception. RequestId: {RequestId}", requestId);

            return View(new ErrorViewModel
            {
                RequestId = requestId,
                Message = "Có lỗi xảy ra khi xử lý yêu cầu. Vui lòng thử lại hoặc liên hệ Admin."
            });
        }

    }
}
