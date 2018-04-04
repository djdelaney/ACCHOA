using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model
{
    public class Response
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [MaxLength(10240)]
        public string Comments { get; set; }

        [Required]
        public Submission Submission { get; set; }
    }
}
