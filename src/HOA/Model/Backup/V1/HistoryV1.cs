using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public class HistoryV1
    {
        public string User { get; set; }
        
        public DateTime DateTime { get; set; }
        
        public string Action { get; set; }
        
        public int Revision { get; set; }
    }
}
