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
        public int CurrentReviewCount { get; set; }
        public bool Reviewed { get; set; }
    }

    public class ApproveRejectViewModel : IValidatableObject
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [Display(Name = "Application Complete?")]
        public bool Approve { get; set; }

        [Required]
        public string Comments { get; set; }

        [Display(Name = "User Feedback")]
        public string UserFeedback { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Approve && string.IsNullOrEmpty(UserFeedback))
            {
                yield return new ValidationResult("You must supply user feedback for rejections.");
            }
        }
    }

    public class TallyVotesViewModel : IValidatableObject
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Comments { get; set; }

        [Display(Name = "User Feedback")]
        public string UserFeedback { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var status = (ReviewStatus)Enum.Parse(typeof(ReviewStatus), Status);
            if (status == ReviewStatus.MissingInformation && string.IsNullOrEmpty(UserFeedback))
            {
                yield return new ValidationResult("You must supply user feedback for missing info.");
            }
        }
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
        [Display(Name = "Brief Description")]
        public string Description { get; set; }

        [Required]
        public IList<IFormFile> Files { get; set; }
    }

    public class StatusViewModel
    {
        [Required]
        public string Code{ get; set; }
    }

    public class ResubmitViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Description { get; set; }
        
        public IList<IFormFile> Files { get; set; }
    }

    public class FinalResponseViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        public string Comments { get; set; }
    }

    public class FinalReview : IValidatableObject
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Comments { get; set; }

        [Display(Name = "User Feedback")]
        public string UserFeedback { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var status = (ReviewStatus)Enum.Parse(typeof(ReviewStatus), Status);
            if ((status == ReviewStatus.ConditionallyApproved || status == ReviewStatus.MissingInformation || status == ReviewStatus.Rejected) && string.IsNullOrEmpty(UserFeedback))
            {
                yield return new ValidationResult("You must supply user feedback for non approvals.");
            }
        }
    }
}
