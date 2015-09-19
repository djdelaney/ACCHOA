using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.ViewModel
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class ManageViewModel
    {
        public List<UserViewModel> Users { get; set; }
    }

    public class UserViewModel
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public bool Enabled { get; set; }
        public string Roles { get; set; }
        public string UserId { get; set; }
    }


}
