using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using HOA.Model;
using HOA.Model.ViewModel;
using Microsoft.AspNet.Identity.EntityFramework;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _roleManager = roleManager;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            var message = String.Format("Logged in as: {0}", User.Identity.Name);
            return Content(message);
            //return View();
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.FindByIdAsync(Context.User.GetUserId());
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            //Remove later - Add Sample Data
            var sampleData = new SampleData(_applicationDbContext, _userManager, _roleManager);
            sampleData.InitializeData();

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                //User not enabled
                var user = _applicationDbContext.Users.FirstOrDefault(u => u.UserName.Equals(model.Username));
                if (user != null && !user.Enabled)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }


                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult ManageUsers(string returnUrl = null)
        {
            var users = _applicationDbContext.Users.Include(u => u.Roles).ToList();

            var model = new ManageViewModel
            {
                Users = new List<UserViewModel>()
            };

            foreach (var user in users)
            {
                List<string> roles = new List<string>();

                
                foreach (var role in user.Roles)
                {
                    var roleName = _roleManager.FindByIdAsync(role.RoleId).Result.Name;

                    if (roleName.Equals(RoleNames.Administrator))
                        roleName = "Administrator";
                    else if (roleName.Equals(RoleNames.CommunityManager))
                        roleName = "Community Manager";
                    else if (roleName.Equals(RoleNames.BoardChairman))
                        roleName = "Board Chairman";
                    else if (roleName.Equals(RoleNames.BoardMember))
                        roleName = "Board Member";
                    else if (roleName.Equals(RoleNames.HOALiaison))
                        roleName = "HOA Liaison";

                    roles.Add(roleName);
                }

                var u = new UserViewModel
                {
                    UserName = user.UserName,
                    Enabled = user.Enabled,
                    FullName = user.FullName,
                    Roles = string.Join(", ", roles),
                    UserId = user.Id
                };

                model.Users.Add(u);
            }
            return View(model);
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult EnableUser(string id)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return HttpNotFound("User not found");

            user.Enabled = true;
            _applicationDbContext.SaveChanges();

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult DisableUser(string id)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return HttpNotFound("User not found");

            user.Enabled = false;
            _applicationDbContext.SaveChanges();

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

    }
}
