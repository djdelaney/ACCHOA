using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public class ReviewV1
    {   
        public string ReviewerEmail { get; set; }
        
        public string Status { get; set; }
        
        public DateTime Created { get; set; }
        
        public string Comments { get; set; }
        
        public int SubmissionRevision { get; set; }
    }
}
