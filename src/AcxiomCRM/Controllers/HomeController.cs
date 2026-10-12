using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Dashboard");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
