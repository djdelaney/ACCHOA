using HOA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace HOA.Util
{
    public static class DBUtil
    {
        public static string GenerateUniqueCode(ApplicationDbContext db)
        {
            string code = "";
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            do
            {
                code = new string(
                    Enumerable.Repeat(chars, 5)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());
            }
            while (db.Submissions.Any(s => s.Code.Equals(code)));
            return code;
        }

        public static List<ApplicationUser> GetRoleMembers(ApplicationDbContext db, string roleName)
        {
            IdentityRole role = db.Roles.Where(r => r.Name.Equals(RoleNames.ARBBoardMember)).FirstOrDefault();
            List<string> userIds = db.UserRoles.Where(r => r.RoleId.Equals(role.Id)).Select(u => u.UserId).ToList();
            return db.Users.Where(u => userIds.Contains(u.Id)).ToList();
        }

        public static List<IdentityRole> GetUserRoles(ApplicationDbContext db, ApplicationUser user)
        {
            List<IdentityRole> roles = db.Roles.ToList();
            List<string> roleMembership = db.UserRoles.Where(c => c.UserId.Equals(user.Id)).Select(u => u.RoleId).ToList();
            return roles.Where(r => roleMembership.Contains(r.Id)).ToList();
        }
    }
}
