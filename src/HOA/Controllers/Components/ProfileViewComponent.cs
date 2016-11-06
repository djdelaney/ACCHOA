using HOA.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HOA.Controllers.Components
{
    public class ProfileViewComponent : ViewComponent
    {
        readonly UserManager<ApplicationUser> userManager;

        public ProfileViewComponent(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }
        
        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var appUser = await userManager.GetUserAsync(HttpContext.User);
                return View(appUser ?? null);
            }
            catch (Exception)
            {
                return View(null);
            }
        }
    }
}
