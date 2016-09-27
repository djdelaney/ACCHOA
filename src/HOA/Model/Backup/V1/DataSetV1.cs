using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public class DataSetV1
    {
        public List<UserV1> Users { get; set; }

        public List<SubmissionV1> Submissions { get; set; }        
    }
}
