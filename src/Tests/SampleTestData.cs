using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HOA.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Tests
{
    public static class SampleTestData
    {
        public static void SetupUsersAndRoles(ApplicationDbContext db)
        {
            ApplicationUser josh = new ApplicationUser()
            {
                FirstName = "Josh",
                LastName = "Rozzi",
                Email = "josh.rozzi@fsresidential.com",
                Enabled = true
            };
            db.Users.Add(josh);

            ApplicationUser kirk = new ApplicationUser()
            {
                FirstName = "Kirk",
                LastName = "Finnis",
                Email = "kfinnis@gmail.com",
                Enabled = true
            };
            db.Users.Add(kirk);

            ApplicationUser dan = new ApplicationUser()
            {
                FirstName = "Dan",
                LastName = "Letscher",
                Email = "dletscher@brenntag.com",
                Enabled = true
            };
            db.Users.Add(dan);

            ApplicationUser deana = new ApplicationUser()
            {
                FirstName = "Deana",
                LastName = "Clymer",
                Email = "deanaclymer@verizon.net",
                Enabled = true
            };
            db.Users.Add(deana);

            ApplicationUser sergio = new ApplicationUser()
            {
                FirstName = "Sergio",
                LastName = "Carrillo",
                Email = "sergio.carrillo@alumni.duke.edu",
                Enabled = true
            };
            db.Users.Add(sergio);

            ApplicationUser melissa = new ApplicationUser()
            {
                FirstName = "Melissa",
                LastName = "Parada",
                Email = "mellomba0526@gmail.com",
                Enabled = true
            };
            db.Users.Add(melissa);

            ApplicationUser admin = new ApplicationUser()
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@applecrossarb.com",
                Enabled = true
            };
            db.Users.Add(admin);

            ApplicationUser tom = new ApplicationUser()
            {
                FirstName = "Admin",
                LastName = "McClung",
                Email = "tom.mcclung@verizon.net",
                Enabled = true,
                LandscapingMember = true
            };
            db.Users.Add(tom);

            ApplicationUser tom2 = new ApplicationUser()
            {
                FirstName = "Admin",
                LastName = "Krewatch",
                Email = "tskrewatch@verizon.net",
                Enabled = true,
                LandscapingMember = true
            };
            db.Users.Add(tom2);

            //Setup roles
            IdentityRole adminRole = new IdentityRole(RoleNames.Administrator);
            db.Roles.Add(adminRole);

            IdentityRole managerRole = new IdentityRole(RoleNames.CommunityManager);
            db.Roles.Add(managerRole);

            IdentityRole chairmanRole = new IdentityRole(RoleNames.BoardChairman);
            db.Roles.Add(chairmanRole);

            IdentityRole arbMemberRole = new IdentityRole(RoleNames.ARBBoardMember);
            db.Roles.Add(arbMemberRole);

            IdentityRole liaisonRole = new IdentityRole(RoleNames.HOALiaison);
            db.Roles.Add(liaisonRole);

            IdentityRole boardMemberRole = new IdentityRole(RoleNames.HOABoardMember);
            db.Roles.Add(boardMemberRole);
            
            //Populate IDs of new roles/users
            db.SaveChanges();

            //Add memberships
            AddUserToRole(db, admin, adminRole);
            AddUserToRole(db, josh, managerRole);
            AddUserToRole(db, kirk, chairmanRole);
            AddUserToRole(db, dan, arbMemberRole);
            AddUserToRole(db, deana, arbMemberRole);
            AddUserToRole(db, sergio, arbMemberRole);
            AddUserToRole(db, melissa, liaisonRole);
            AddUserToRole(db, tom, arbMemberRole);
            AddUserToRole(db, tom2, arbMemberRole);

            db.SaveChanges();
        }

        private static void AddUserToRole(ApplicationDbContext db, ApplicationUser user, IdentityRole role)
        {
            IdentityUserRole<string> membership = new IdentityUserRole<string>()
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            //role.Users.Add(membership);
            db.UserRoles.Add(membership);
        }
        
    }
}
