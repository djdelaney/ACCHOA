using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public class StateChangeV1
    {
        public DateTime StartTime { get; set; }
        
        public DateTime EndTime { get; set; }
        
        public string State { get; set; }
    }
}
