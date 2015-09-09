using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using HOA.Model;
using Microsoft.Data.Entity;
using Microsoft.AspNet.Identity;

namespace HOA.Controllers
{
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
            EnsureDatabaseCreated(_applicationDbContext);

            InitData(_userManager, _applicationDbContext);

            return View();
        }

        public IActionResult Error()
        {
            
            return Content("ERROR");
            //return View("~/Views/Shared/Error.cshtml");
        }

        

        // The following code creates the database and schema if they don't exist.
        // This is a temporary workaround since deploying database through EF migrations is
        // not yet supported in this release.
        // Please see this http://go.microsoft.com/fwlink/?LinkID=615859 for more information on how to do deploy the database
        // when publishing your application.
        private static bool _databaseChecked;
        private static void EnsureDatabaseCreated(ApplicationDbContext context)
        {
            if (!_databaseChecked)
            {
                _databaseChecked = true;
                context.Database.Migrate();
            }
        }

        private static async void InitData(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            if (context.Users.Count() > 0)
                return;

            var user = new ApplicationUser
            {
                UserName = "dan",
                FullName = "Daniel"
            };

            IdentityResult result = await userManager.CreateAsync(user, "Bentley2015!");

        }
    }
}
