using Microsoft.AspNetCore.Mvc;

namespace SimpleExampleInvoice.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
