using Microsoft.AspNet.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Util
{
    public class AuthorizeRoles : AuthorizeAttribute
    {
        public AuthorizeRoles(params string[] roles)
        {
            if (roles.Any())
                Roles = string.Join(",", roles);
        }        
    }
}
