using HOA.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Util
{
    public static class DBUtil
    {
        public static string GenerateUniqueCode(ApplicationDbContext db)
        {
            string code = "";
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            do
            {
                code = new string(
                    Enumerable.Repeat(chars, 5)
                              .Select(s => s[random.Next(s.Length)])
                              .ToArray());
            }
            while (db.Submissions.Any(s => s.Code.Equals(code)));
            return code;
        }
    }
}
