using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Http;

namespace HOA.Model.ViewModel
{
    public class ViewSubmissionsViewModel
    {
        public string Filter { get; set; }
        public IList<Submission> Submissions { get; set; }
    }

    public class ViewSubmissionViewModel
    {
        public Submission Submission { get; set; }
        public int ReviewerCount { get; set; }
        public bool Reviewed { get; set; }
    }

    public class ApproveRejectViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public bool Approve { get; set; }

        [Required]
        public string Comments { get; set; }
    }

    public class ReviewSubmissionViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Status{ get; set; }

        [Required]
        public string Comments { get; set; }
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
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required]
        public string Description { get; set; }

        [Required]
        public IList<IFormFile> Files { get; set; }
    }

    public class StatusViewModel
    {
        [Required]
        public string Code{ get; set; }
    }
}
