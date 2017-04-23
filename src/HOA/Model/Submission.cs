using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model
{
    public enum Status
    {
        CommunityMgrReview = 0,
        ARBChairReview = 1,
        CommitteeReview = 2,
        ARBTallyVotes = 3,
        HOALiasonReview = 4,
        FinalResponse = 5,

        CommunityMgrReturn = 6,

        HOALiasonInput = 7,

        Rejected = 8,
        MissingInformation = 9,
        Approved = 10,
        ConditionallyApproved = 11,
        Retracted = 12
    }

    public enum ReturnStatus
    {
        None = 0,
        Reject = 1,
        MissingInformation = 2,
        Approved = 3,
        ConditionallyApproved = 4
    }

    public class Submission
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Code { get; set; }

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
        [MaxLength(10240)]
        public string Description { get; set; }

        [Required]
        public Status Status { get; set; }

        [Required]
        public ReturnStatus ReturnStatus { get; set; }

        [Required]
        public int Revision { get; set; }

        [Required]
        public DateTime LastModified { get; set; }

        [Required]
        public DateTime StatusChangeTime { get; set; }

        [Required]
        public DateTime SubmissionDate { get; set; }

        [Required]
        public bool PrecedentSetting { get; set; }

        public virtual IList<Review> Reviews { get; set; }

        public virtual IList<History> Audits { get; set; }

        public virtual IList<Response> Responses { get; set; }

        public virtual IList<File> Files { get; set; }

        public virtual IList<StateChange> StateHistory { get; set; }

        public virtual IList<Comment> Comments { get; set; }

        public string ResponseDocumentBlob { get; set; }

        public string ResponseDocumentFileName { get; set; }

        [NotMapped]
        public TimeSpan ElapsedTime
        {
            get
            {
                //Only show current time for open items
                if (Status != Status.Approved &&
                    Status != Status.ConditionallyApproved &&
                    Status != Status.MissingInformation &&
                    Status != Status.Rejected &&
                    Status != Status.Retracted)
                {
                    return DateTime.Now.Subtract(SubmissionDate);
                }

                return LastModified.Subtract(SubmissionDate);
            }
        }
    }
}
