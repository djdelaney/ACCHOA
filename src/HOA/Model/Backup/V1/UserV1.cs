using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Model.Backup.V1
{
    public class UserV1
    {
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        public bool Enabled { get; set; }
        
        public bool DisableNotification { get; set; }

        public List<String> Roles { get; set; }

        public string Email { get; set; }
    }
}
