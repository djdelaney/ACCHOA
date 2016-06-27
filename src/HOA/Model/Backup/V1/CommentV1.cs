using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public class CommentV1
    {        
        public string UserEmail { get; set; }
        
        public DateTime Created { get; set; }
        
        public string Comments { get; set; }
    }
}
