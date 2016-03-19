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
        Submitted = 0,
        ARBIncoming = 1,
        UnderReview = 2,
        ARBFinal = 3,
        ReviewComplete = 4,
        PrepApproval = 5,
        PrepConditionalApproval = 6,
        Rejected = 7,
        MissingInformation = 8,
        Approved = 9,
        ConditionallyApproved = 10,
        Retracted = 11
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
        [MaxLength(256)]
        public string Description { get; set; }

        [Required]
        public Status Status { get; set; }

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

        [NotMapped]
        public TimeSpan ElapsedTime
        {
            get
            {
                return DateTime.Now.Subtract(SubmissionDate);
            }
        }
    }
}
