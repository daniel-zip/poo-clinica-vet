using Microsoft.AspNetCore.Mvc;

namespace VetCare.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View();
    }
}
