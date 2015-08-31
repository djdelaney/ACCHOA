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
        public IList<Submission> NewSubmissions { get; set; }
        public IList<Submission> ARBIncoming { get; set; }
        public IList<Submission> ForReview { get; set; }
        public IList<Submission> ARBFinal { get; set; }
        public IList<Submission> FinalApproval { get; set; }
    }

    public class ViewSubmissionViewModel
    {
        public Submission Submission { get; set; }
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
    }

}
