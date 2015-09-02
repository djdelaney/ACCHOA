using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model
{
    public class History
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public String User { get; set; }

        [Required]
        public DateTime DateTime { get; set; }

        [Required]
        [MaxLength(64)]
        public string Action { get; set; }

        [Required]
        public Submission Submission { get; set; }
    }
}
