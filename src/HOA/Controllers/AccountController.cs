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
using Microsoft.AspNet.Hosting;

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
        private IHostingEnvironment _env;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            RoleManager<IdentityRole> roleManager,
            IHostingEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _roleManager = roleManager;
            _env = env;
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
            return await _userManager.FindByIdAsync(Request.HttpContext.User.GetUserId());
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
            //Setup default admin user and roles
            var sampleData = new SampleData(_applicationDbContext, _userManager, _roleManager);
            sampleData.InitializeData(_env.IsDevelopment());

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = "/Submission/List";

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
            var users = _applicationDbContext.Users.Include(u => u.Roles).OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();

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

        [HttpGet]
        [AllowAnonymous]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existing = _userManager.FindByEmailAsync(model.Email).Result;
                if (existing != null)
                {
                    ModelState.AddModelError("Email", "A user with that email already exists");
                    return View(model);
                }

                existing = _userManager.FindByNameAsync(model.UserName).Result;
                if (existing != null)
                {
                    ModelState.AddModelError("UserName", "A user with that username already exists");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Enabled = true
                };
                await _userManager.CreateAsync(user, model.Password);
                await _userManager.AddToRoleAsync(user, model.Role);

                _applicationDbContext.SaveChanges();

                return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

    }
}
