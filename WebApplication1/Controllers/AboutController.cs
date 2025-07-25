using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

    public class AboutController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

    }