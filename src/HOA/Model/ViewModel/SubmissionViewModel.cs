using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc.Rendering;

namespace HOA.Model.ViewModel
{
    public class ViewSubmissionsViewModel
    {
        public IList<Submission> Submissions { get; set; }
    }

    public class ViewSubmissionViewModel
    {
        public Submission Submission { get; set; }
    }

    public class CreateSubmissionViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "House Number")]
        public int HouseNumber { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Street Name")]
        public string StreetName { get; set; }
        
        [Required]
        public string Description { get; set; }
    }

}
