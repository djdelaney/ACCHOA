﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.ViewModel
{
    public class LoginViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

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
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool Enabled { get; set; }
        public bool DisableNotification { get; set; }
        public string Roles { get; set; }
        public string UserId { get; set; }
        public bool LandscapingMember { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First  Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last  Name")]
        public string LastName { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        [Display(Name = "Landscaping reviewer")]
        public bool IsLandscaping { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class PasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [Display(Name = "New password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class EditUserViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First  Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last  Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Admin")]
        public bool IsAdmin { get; set; }

        [Required]
        [Display(Name = "Landscaping Member")]
        public bool IsLandscaping { get; set; }

        [Required]
        [Display(Name = "ARB Member")]
        public bool IsARBMember { get; set; }

        [Required]
        [Display(Name = "ARB Chair")]
        public bool IsArbChair { get; set; }

        [Required]
        [Display(Name = "Community Manager")]
        public bool IsCommunityManager { get; set; }

        [Required]
        [Display(Name = "HOA Liaison")]
        public bool IsHoaLiaison { get; set; }

        [Required]
        [Display(Name = "HOA Member")]
        public bool IsHoaMember { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string ResetCode { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [Display(Name = "New password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class DeleteUserViewModel
    {
        [Required]
        public string UserId { get; set; }
    }
}
