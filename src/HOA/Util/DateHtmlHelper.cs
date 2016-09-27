using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace HOA.Util
{
    public class DateHtmlHelper
    {
        public static string ConvertToLocalDate(DateTime dt, string format)
        {
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(dt, easternZone);
            return local.ToString(format);
        }
    }
}
