using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model
{
    public enum Status
    {
        Submitted,
        Rejected,
        UnderReview,
        ReviewComplete,
        Approved
    }

    public class Submission
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(32)]
        public string LastName { get; set; }

        [Required]
        public int HouseNumber { get; set; }

        [Required]
        [MaxLength(64)]
        public string StreetName { get; set; }

        [Required]
        [MaxLength(64)]
        public string Email { get; set; }

        [Required]
        [MaxLength(256)]
        public string Description { get; set; }

        [Required]
        public Status Status { get; set; }

        public virtual IList<Review> Reviews { get; set; }

        public virtual IList<History> Audits { get; set; }
    }
}
