using Microsoft.AspNetCore.Mvc;
using TourGuideAdmin.Models;
using System.Diagnostics;

namespace TourGuideAdmin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}