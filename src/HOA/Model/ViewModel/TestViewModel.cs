using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.ViewModel
{
    public class CreateTestViewModel
    {
        [Required]
        public string Type { get; set; }

        [Required]
        public int Count { get; set; }
    }

    public class DeleteAllViewModel
    {
        public bool Approve { get; set; }
    }

    public class CreateRandomViewModel
    {
        [Required]
        public int Count { get; set; }
    }
}
