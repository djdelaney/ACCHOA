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

            role = await _roleManager.FindByNameAsync(RoleNames.ARBBoardMember);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.ARBBoardMember));
            }

            role = await _roleManager.FindByNameAsync(RoleNames.HOALiaison);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.HOALiaison));
            }

            role = await _roleManager.FindByNameAsync(RoleNames.HOABoardMember);
            if (role == null)
            {
                await _roleManager.CreateAsync(new IdentityRole(RoleNames.HOABoardMember));
            }            
        }

        private async Task CreateUsers(bool isDevelopment)
        {
            var user = await _userManager.FindByNameAsync("admin");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "admin", Email = "dan@hactar.com", FirstName = "Admin", LastName = "Admin", Enabled = true };
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
            user = await _userManager.FindByEmailAsync("josh.rozzi@fsresidential.com");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "josh.rozzi@fsresidential.com", Email = "josh.rozzi@fsresidential.com", FirstName = "Josh", LastName = "Rozzi", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.CommunityManager);
            }

            user = await _userManager.FindByEmailAsync("kfinnis@gmail.com");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "kfinnis@gmail.com", Email = "kfinnis@gmail.com", FirstName = "Kirk", LastName = "Finnis", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.BoardChairman);
            }

            user = await _userManager.FindByEmailAsync("dletscher@brenntag.com");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "dletscher@brenntag.com", Email = "dletscher@brenntag.com", FirstName = "Dan", LastName = "Letscher", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.ARBBoardMember);
            }

            user = await _userManager.FindByEmailAsync("kkrama06@gmail.com");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "kkrama06@gmail.com", Email = "kkrama06@gmail.com", FirstName = "Keith", LastName = "Rama", Enabled = true };
                await _userManager.CreateAsync(user, "Password");
                await _userManager.AddToRoleAsync(user, RoleNames.HOALiaison);
            }
            */
        }
    }
}
