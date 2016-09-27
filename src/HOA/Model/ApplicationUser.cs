using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace HOA.Model
{
    public static class RoleNames
    {
        public const string Administrator       = "Administrator";
        public const string CommunityManager    = "CommunityManager";
        public const string BoardChairman       = "BoardChairman";
        public const string ARBBoardMember      = "ARBBoardMember";
        public const string HOALiaison          = "HOALiaison";
        public const string HOABoardMember      = "HOABoardMember";
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(20)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(20)]
        public string LastName { get; set; }

        [Required]
        public bool Enabled { get; set; }

        [Required]
        public bool DisableNotification { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                return string.Format("{0} {1}", FirstName, LastName);
            }
        }
    }
}
