using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HOA.Model;
using HOA.Model.ViewModel;
using HOA.Util;
using HOA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [RequireHttps]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _email;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext applicationDbContext,
            RoleManager<IdentityRole> roleManager,
            IEmailSender email)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _applicationDbContext = applicationDbContext;
            _roleManager = roleManager;
            _email = email;
        }
        
        public IActionResult Index()
        {
            return View(GetCurrentUserAsync().Result);
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(HttpContext.User);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
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
            bool isDevEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            sampleData.InitializeData(isDevEnv);

            if (string.IsNullOrEmpty(returnUrl))
                returnUrl = "/Submission/List";

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                //User not enabled
                var user = _applicationDbContext.Users.FirstOrDefault(u => u.Email.Equals(model.Email));
                if (user == null || !user.Enabled)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
                
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
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
            var users = _applicationDbContext.Users.ToList();

            var model = new ManageViewModel
            {
                Users = new List<UserViewModel>()
            };

            var identityUsers = _userManager.Users.ToList();

            foreach (var user in users)
            {
                List<string> roles = new List<string>();

                List<IdentityRole> identityRoles = DBUtil.GetUserRoles(_applicationDbContext, user);
                
                foreach (var role in identityRoles)
                {
                    //var roleName = _roleManager.FindByIdAsync(role.Name).Result.Name;
                    var roleName = role.Name;

                    if (roleName.Equals(RoleNames.Administrator))
                        roleName = "Administrator";
                    else if (roleName.Equals(RoleNames.CommunityManager))
                        roleName = "Community Manager";
                    else if (roleName.Equals(RoleNames.BoardChairman))
                        roleName = "Board Chairman";
                    else if (roleName.Equals(RoleNames.ARBBoardMember))
                        roleName = "ARB Board Member";
                    else if (roleName.Equals(RoleNames.HOALiaison))
                        roleName = "HOA Liaison";

                    roles.Add(roleName);
                }

                var u = new UserViewModel
                {
                    Enabled = user.Enabled,
                    DisableNotification = user.DisableNotification,
                    FullName = user.FullName,
                    Roles = string.Join(", ", roles),
                    UserId = user.Id,
                    Email = identityUsers.FirstOrDefault(iu => iu.Id == user.Id).Email,
                    LandscapingMember = user.LandscapingMember
                };

                model.Users.Add(u);
            }

            model.Users = model.Users.OrderBy(u => u.Roles).ToList();

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Delete(string id)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return NotFound("User not found");

            DeleteUserViewModel model = new DeleteUserViewModel()
            {
                UserId = id
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> Delete(DeleteUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);

                var comments = _applicationDbContext.Comments.Where(c => c.User == user);
                var reviews = _applicationDbContext.Reviews.Where(r => r.Reviewer == user);

                if (comments.Count() > 0 || reviews.Count() > 0)
                {
                    throw new Exception("CANNOT DELETE");
                }

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

            return View(model);
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Disable(string id, bool disable)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return NotFound("User not found");

            user.Enabled = !disable;
            _applicationDbContext.SaveChanges();

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult DisableNotifications(string id, bool disable)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return NotFound("User not found");

            user.DisableNotification = disable;
            _applicationDbContext.SaveChanges();

            return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                var url = this.Url.Action("ResetPassword", new { id = user.Id, code = code });

                EmailHelper.NotifyResetPassword(user.Email, url, _email);

                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string id, string code)
        {
            var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(id));
            if (user == null)
                return NotFound("User not found");
            
            ResetPasswordViewModel model = new ResetPasswordViewModel
            {
                UserId = user.Id,
                ResetCode = code
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _applicationDbContext.Users.FirstOrDefault(u => u.Id.Equals(model.UserId));
                if (user == null)
                    return NotFound("User not found");

                IdentityResult result = await _userManager.ResetPasswordAsync(user, model.ResetCode, model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction(nameof(SubmissionController.List), "Submission");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
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

                existing = _userManager.FindByEmailAsync(model.Email).Result;
                if (existing != null)
                {
                    ModelState.AddModelError("UserName", "A user with that username already exists");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Enabled = true,
                    LandscapingMember = model.IsLandscaping
                };
                await _userManager.CreateAsync(user, model.Password);
                await _userManager.AddToRoleAsync(user, model.Role);

                _applicationDbContext.SaveChanges();

                EmailHelper.NotifyNewUser(model.Email, model.Password, _email);

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
                return NotFound("User not found");

            EditUserViewModel model = new EditUserViewModel
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsLandscaping = user.LandscapingMember
            };

            List<IdentityRole> identityRoles = DBUtil.GetUserRoles(_applicationDbContext, user);

            foreach (var role in identityRoles)
            {
                //var roleName = _roleManager.FindByIdAsync(role.Name).Result.Name;
                var roleName = role.Name;

                if (roleName.Equals(RoleNames.Administrator))
                    model.IsAdmin = true;
                else if (roleName.Equals(RoleNames.CommunityManager))
                    model.IsCommunityManager = true;
                else if (roleName.Equals(RoleNames.BoardChairman))
                    model.IsArbChair = true;
                else if (roleName.Equals(RoleNames.ARBBoardMember))
                    model.IsARBMember = true;
                else if (roleName.Equals(RoleNames.HOALiaison))
                    model.IsHoaLiaison = true;
                else if (roleName.Equals(RoleNames.HOABoardMember))
                    model.IsHoaMember = true;
            }

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
                    return NotFound("User not found");

                //changing username
                if(!model.Email.Equals(user.Email))
                {
                    user.UserName = model.Email;
                }
                
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.LandscapingMember = model.IsLandscaping;

                await _userManager.UpdateAsync(user);

                //Update roles?
                List<IdentityRole> identityRoles = DBUtil.GetUserRoles(_applicationDbContext, user);
                
                if(model.IsAdmin && !identityRoles.Any(r => r.Name.Equals(RoleNames.Administrator)))
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.Administrator);
                }
                if(!model.IsAdmin && identityRoles.Any(r => r.Name.Equals(RoleNames.Administrator)))
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.Administrator);
                }

                if (model.IsARBMember && !identityRoles.Any(r => r.Name.Equals(RoleNames.ARBBoardMember)))
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.ARBBoardMember);
                }
                if (!model.IsARBMember && identityRoles.Any(r => r.Name.Equals(RoleNames.ARBBoardMember)))
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.ARBBoardMember);
                }

                if (model.IsArbChair && !identityRoles.Any(r => r.Name.Equals(RoleNames.BoardChairman)))
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.BoardChairman);
                }
                if (!model.IsArbChair && identityRoles.Any(r => r.Name.Equals(RoleNames.BoardChairman)))
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.BoardChairman);
                }

                if (model.IsCommunityManager && !identityRoles.Any(r => r.Name.Equals(RoleNames.CommunityManager)))
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.CommunityManager);
                }
                if (!model.IsCommunityManager && identityRoles.Any(r => r.Name.Equals(RoleNames.CommunityManager)))
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.CommunityManager);
                }

                if (model.IsHoaLiaison && !identityRoles.Any(r => r.Name.Equals(RoleNames.HOALiaison)))
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.HOALiaison);
                }
                if (!model.IsHoaLiaison && identityRoles.Any(r => r.Name.Equals(RoleNames.HOALiaison)))
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.HOALiaison);
                }

                if (model.IsHoaMember && !identityRoles.Any(r => r.Name.Equals(RoleNames.HOABoardMember)))
                {
                    await _userManager.AddToRoleAsync(user, RoleNames.HOABoardMember);
                }
                if (!model.IsHoaMember && identityRoles.Any(r => r.Name.Equals(RoleNames.HOABoardMember)))
                {
                    await _userManager.RemoveFromRoleAsync(user, RoleNames.HOABoardMember);
                }

                return RedirectToAction(nameof(AccountController.ManageUsers), "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

    }
}
