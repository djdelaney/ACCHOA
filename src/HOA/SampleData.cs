using HOA.Model;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA
{
    public class SampleData
    {
        private ApplicationDbContext _ctx;
        private UserManager<ApplicationUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;

        public SampleData(ApplicationDbContext ctx, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _ctx = ctx;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void InitializeData()
        {
            if (_ctx.Database.EnsureCreated())
            {

                CreateRoles().Wait();
                CreateUsers().Wait();
                //CreateSampleData();
            }
        }

        private async Task CreateRoles()
        {
            //_roleManager.RoleExistsAsync
            await _roleManager.CreateAsync(new IdentityRole("Administrator"));
            await _roleManager.CreateAsync(new IdentityRole("CommunityManager"));
            await _roleManager.CreateAsync(new IdentityRole("BoardChairman"));
            await _roleManager.CreateAsync(new IdentityRole("BoardMember"));
            await _roleManager.CreateAsync(new IdentityRole("HOALiaison"));
        }

        private async Task CreateUsers()
        {
            var user = await _userManager.FindByNameAsync("admin");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "admin", Email = "admin@mailinator.com", FullName = "Site Admin", Role = ReviewRole.Admin };
                await _userManager.CreateAsync(user, "P@ssw0rd!");
                await _userManager.AddToRoleAsync(user, "Administrator");
                await _userManager.AddToRoleAsync(user, "CommunityManager");
                await _userManager.AddToRoleAsync(user, "BoardChairman");
                await _userManager.AddToRoleAsync(user, "BoardMember");
                await _userManager.AddToRoleAsync(user, "HOALiaison");
            }

        }
    }
}
