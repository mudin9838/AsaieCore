using Microsoft.AspNetCore.Mvc;

namespace Asaie.Api.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
