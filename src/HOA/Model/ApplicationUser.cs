using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.OptionsModel;
using System.ComponentModel.DataAnnotations;

namespace HOA.Model
{
    public enum ReviewRole
    {
        Admin,
        CommunityManager,
        BoardChairman,
        BoardMember,
        HOALiaison
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(20)]
        public string FullName { get; set; }

        [Required]
        public ReviewRole Role { get; set; }
    }
}
