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
using HOA.Util;
using HOA.Services;

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
        private readonly IEmailSender _email;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            RoleManager<IdentityRole> roleManager,
            IHostingEnvironment env,
            IEmailSender email)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _roleManager = roleManager;
            _env = env;
            _email = email;
        }
        
        public IActionResult Index()
        {
            return View(GetCurrentUserAsync().Result);
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.FindByIdAsync(Request.HttpContext.User.GetUserId());
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new PasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(PasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();
                IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.Password);

                if(!result.Succeeded)
                {
                    string message = "";
                    foreach (IdentityError error in result.Errors)
                    {
                        message += error.Description + " ";
                    }

                    ModelState.AddModelError("Password", message);
                    return View(model);
                }

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
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
                    DisableNotification = user.DisableNotification,
                    FullName = user.FullName,
                    Roles = string.Join(", ", roles),
                    UserId = user.Id
                };

                model.Users.Add(u);
            }

            model.Users = model.Users.OrderBy(u => u.Roles).ToList();

            return View(model);
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            
            var rolesForUser = await _userManager.GetRolesAsync(user);

            if (rolesForUser.Count() > 0)
            {
                foreach (var item in rolesForUser.ToList())
                {
                    // item should be the name of the role
                    var result = await _userManager.RemoveFromRoleAsync(user, item);
                }
            }

            await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Disable(string id, bool disable)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return HttpNotFound("User not found");

            user.Enabled = !disable;
            _applicationDbContext.SaveChanges();

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult DisableNotifications(string id, bool disable)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return HttpNotFound("User not found");

            user.DisableNotification = disable;
            _applicationDbContext.SaveChanges();

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
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

                EmailHelper.NotifyNewUser(model.Email, model.UserName, model.Password, _email);

                return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Edit(string id)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return HttpNotFound("User not found");

            EditUserViewModel model = new EditUserViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                    return HttpNotFound("User not found");

                user.UserName = model.UserName;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;

                await _userManager.UpdateAsync(user);

                return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

    }
}
