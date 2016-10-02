using System;
using HOA.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Diagnostics;

namespace HOA.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;


        public HomeController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
        }

        public IActionResult Index()
        {
            HomeIndexModel model = new HomeIndexModel
            {
                fiveDayTurnaround = StatsController.GetTurnaroundTime(_applicationDbContext)
            };
            return View(model);
        }

        public IActionResult Forms()
        {
            return View();
        }

        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var error = feature?.Error;
            return View(error);
        }
    }

    public class HomeIndexModel
    {
        public int fiveDayTurnaround { get; set; }
    }
}
