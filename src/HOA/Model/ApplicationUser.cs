using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using System.ComponentModel.DataAnnotations;

namespace HOA.Model
{
    public static class RoleNames
    {
        public const string Administrator       = "Administrator";
        public const string CommunityManager    = "CommunityManager";
        public const string BoardChairman       = "BoardChairman";
        public const string BoardMember         = "BoardMember";
        public const string HOALiaison          = "HOALiaison";        
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(20)]
        public string FullName { get; set; }

        [Required]
        public bool Enabled { get; set; }

        [Required]
        public bool DisableNotification { get; set; }
    }
}
