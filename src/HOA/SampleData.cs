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

        public void InitializeData()
        {
            CreateRoles().Wait();
            CreateUsers().Wait();
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

        private async Task CreateUsers()
        {
            var user = await _userManager.FindByNameAsync("admin");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "admin", Email = "admin@mailinator.com", FullName = "Site Admin" };
                await _userManager.CreateAsync(user, "P@ssw0rd!");
                await _userManager.AddToRoleAsync(user, RoleNames.Administrator);
                await _userManager.AddToRoleAsync(user, RoleNames.CommunityManager);
                await _userManager.AddToRoleAsync(user, RoleNames.BoardChairman);
                await _userManager.AddToRoleAsync(user, RoleNames.BoardMember);
                await _userManager.AddToRoleAsync(user, RoleNames.HOALiaison);
            }

            user = await _userManager.FindByNameAsync("CommunityManager");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "CommunityManager", Email = "CommunityManager@mailinator.com", FullName = "Community Manager" };
                await _userManager.CreateAsync(user, "P@ssw0rd!");
                await _userManager.AddToRoleAsync(user, RoleNames.CommunityManager);
            }

            user = await _userManager.FindByNameAsync("BoardMember1");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "BoardMember1", Email = "BoardMember1@mailinator.com", FullName = "Board Member1" };
                await _userManager.CreateAsync(user, "P@ssw0rd!");
                await _userManager.AddToRoleAsync(user, RoleNames.BoardMember);
            }

        }
    }
}
