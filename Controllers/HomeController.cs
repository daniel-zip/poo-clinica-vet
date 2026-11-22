using Microsoft.AspNetCore.Mvc;

namespace VetCare.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
