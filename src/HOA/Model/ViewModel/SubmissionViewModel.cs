using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using HOA.Util;
using Microsoft.AspNetCore.Http;

namespace HOA.Model.ViewModel
{
    public class ViewSubmissionsViewModel
    {
        public string Filter { get; set; }
        public IList<Submission> Submissions { get; set; }
        public Pager Pager { get; set; }
    }

    public class ViewSubmissionViewModel
    {
        public Submission Submission { get; set; }
        public int ReviewerCount { get; set; }
        public int CurrentReviewCount { get; set; }
        public bool Reviewed { get; set; }
    }

    public class CheckCompletenessViewModel : IValidatableObject
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [Display(Name = "Application Complete?")]
        public bool Approve { get; set; }
        
        [Display(Name = "Internal Comments")]
        [MaxLength(512)]
        public string Comments { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Approve && string.IsNullOrEmpty(Comments))
            {
                yield return new ValidationResult("You must supply internal comments for rejections.");
            }
        }
    }

    public enum TallyStatus
    {
        Approved,
        Rejected,
        ConditionallyApproved,
        MissingInformation,
        HOAInputRequired
    }

    public class TallyVotesViewModel : IValidatableObject
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Status { get; set; }
        
        [Display(Name = "Internal Comments")]
        public string Comments { get; set; }
        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var status = (TallyStatus)Enum.Parse(typeof(TallyStatus), Status);
            if(status != TallyStatus.Approved && string.IsNullOrEmpty(Comments))
            {
                yield return new ValidationResult("You must supply internal comments for non-approvals.");
            }
        }
    }

    public class ReviewSubmissionViewModel : IValidatableObject
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Status{ get; set; }
        
        [Display(Name = "Internal Comments")]
        public string Comments { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Enum.TryParse(Status, out ReviewStatus statusEnum))
            {
                if (statusEnum != ReviewStatus.Approved && string.IsNullOrEmpty(Comments))
                {
                    yield return new ValidationResult("You must supply comments for non-approvals.");
                }
            }
        }
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

        [Required]
        public string UserFeedback{ get; set; }

        [Required]
        [Display(Name = "Internal Comments")]
        public string Comments { get; set; }

        public IList<IFormFile> Files { get; set; }
    }

    public class ReturnCommentsViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string UserFeedback { get; set; }
    }

    public class FinalReview
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Status { get; set; }
        
        [Display(Name = "Internal Comments")]
        public string Comments { get; set; }
    }

    public class EditSubmissionViewModel
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [MaxLength(32)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(32)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(64)]
        public string Address { get; set; }

        [Required]
        [MaxLength(64)]
        public string Email { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Description { get; set; }

        public IList<IFormFile> Files { get; set; }
    }

    public class SearchViewModel : IValidatableObject
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if(string.IsNullOrEmpty(Name) &&
                string.IsNullOrEmpty(Address) &&
                string.IsNullOrEmpty(Code) &&
                string.IsNullOrEmpty(Description))
            {
                yield return new ValidationResult("You must enter search terms");
            }
        }
    }

    public class SearchResultsViewModel
    {
        public IList<Submission> Submissions { get; set; }
    }

    public class QuickApproveViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string UserFeedback { get; set; }

        [Required]
        [Display(Name = "Internal Comments")]
        public string Comments { get; set; }

        public IList<IFormFile> Files { get; set; }
    }

    public class AttachmentViewModel
    {
        public Submission Submission { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string UserFeedback { get; set; }

        public IList<IFormFile> Files { get; set; }
    }
}
