using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

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
