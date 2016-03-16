using HOA.Model;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
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

        public void InitializeData(bool isDevelopment)
        {
            CreateRoles().Wait();
            CreateUsers(isDevelopment).Wait();
        }

        private async Task CreateRoles()
        {
            var role = await _roleManager.FindByNameAsync(RoleNames.Administrator);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.Administrator));
            }

            role = await _roleManager.FindByNameAsync(RoleNames.CommunityManager);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.CommunityManager));
            }

            role = await _roleManager.FindByNameAsync(RoleNames.BoardChairman);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.BoardChairman));
            }

            role = await _roleManager.FindByNameAsync(RoleNames.BoardMember);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.BoardMember));
            }

            role = await _roleManager.FindByNameAsync(RoleNames.HOALiaison);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.HOALiaison));
            }
        }

        private async Task CreateUsers(bool isDevelopment)
        {
            var user = await _userManager.FindByNameAsync("admin");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "admin", Email = "admin@mailinator.com", FirstName = "Admin", LastName = "Admin", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.Administrator);
            }
            else
            {
                return;
            }

            //Only create other users for dev purposes
            if (!isDevelopment)
                return;
            
            /*
            user = await _userManager.FindByNameAsync("JoshRozzi");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "JoshRozzi", Email = "JoshRozzi@mailinator.com", FirstName = "Josh", LastName = "Rozzi", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.CommunityManager);
            }

            user = await _userManager.FindByNameAsync("KirkFinnis");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "KirkFinnis", Email = "KirkFinnis@mailinator.com", FirstName = "Kirk", LastName = "Finnis", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.BoardChairman);
            }

            user = await _userManager.FindByNameAsync("DanLetscher");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "DanLetscher", Email = "DanLetscher@mailinator.com", FirstName = "Dan", LastName = "Letscher", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.BoardMember);
            }

            user = await _userManager.FindByNameAsync("HOALiaison");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "HOALiaison", Email = "HOALiaison@mailinator.com", FirstName = "HOA", LastName = "Liaison", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.HOALiaison);
            }*/

        }
    }
}
