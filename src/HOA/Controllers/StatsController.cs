using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HOA.Model;
using HOA.Model.ViewModel;
using HOA.Util;
using HOA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [RequireHttps]
    [Authorize]
    public class StatsController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public StatsController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            List<Tuple<string, int>> submissionsByMonth = new List<Tuple<string, int>>();

            DateTime minTime = DateTime.Now.AddYears(-1);
            List<DateTime> dates = _applicationDbContext.Submissions.Where(s=> s.SubmissionDate > minTime).Select(s => s.SubmissionDate).ToList();

            var grouppedResult = dates.GroupBy(x => x.Month).OrderBy(x => x.Key);

            DateTime curMonth = DateTime.Now.AddMonths(-11);
            for(int x=0; x<12; x++)
            {
                int month = curMonth.Month;

                System.Globalization.DateTimeFormatInfo mfi = new System.Globalization.DateTimeFormatInfo();
                string monthName = mfi.GetMonthName(month);
                int monthCount = 0;
                var data = grouppedResult.FirstOrDefault(g => g.Key == month);
                if(data != null)
                {
                    monthCount = data.Count();
                }
                
                curMonth = curMonth.AddMonths(1);

                submissionsByMonth.Add(new Tuple<string, int>(monthName, monthCount));
            }

            StatsModel model = new StatsModel
            {
                ByMonth = submissionsByMonth
            };

            return View(model);
        }
    }

    public class StatsModel
    {
        public List<Tuple<string, int>> ByMonth { get; set; }
    }
}
