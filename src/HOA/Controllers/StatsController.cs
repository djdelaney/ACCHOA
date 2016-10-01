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
            StatsModel model = new StatsModel
            {
                ByMonth = GetSubmissionsByMonth(),
                ResponseCounts = GetResponseCounts(),
                ElapsedTime = GetTurnaroundTime(),
                TotalSubmissions = _applicationDbContext.Submissions.Count(),
                DaysByCategory = TimeByStage()
            };

            //Average open time 

            //pie chart of responses

            return View(model);
        }

        

        private List<Tuple<string, int>> GetSubmissionsByMonth()
        {
            List<Tuple<string, int>> submissionsByMonth = new List<Tuple<string, int>>();

            DateTime minTime = DateTime.Now.AddYears(-1);
            List<DateTime> dates = _applicationDbContext.Submissions.Where(s => s.SubmissionDate > minTime).Select(s => s.SubmissionDate).ToList();

            var grouppedResult = dates.GroupBy(x => x.Month).OrderBy(x => x.Key);

            DateTime curMonth = DateTime.Now.AddMonths(-11);
            for (int x = 0; x < 12; x++)
            {
                int month = curMonth.Month;

                System.Globalization.DateTimeFormatInfo mfi = new System.Globalization.DateTimeFormatInfo();
                string monthName = mfi.GetMonthName(month);
                int monthCount = 0;
                var data = grouppedResult.FirstOrDefault(g => g.Key == month);
                if (data != null)
                {
                    monthCount = data.Count();
                }

                curMonth = curMonth.AddMonths(1);

                submissionsByMonth.Add(new Tuple<string, int>(monthName, monthCount));
            }

            return submissionsByMonth;
        }

        private ResponseCount GetResponseCounts()
        {
            ResponseCount results = new ResponseCount();
            List<Submission> submissions = _applicationDbContext.Submissions
                                                                .Where(s => s.Status == Status.Approved ||
                                                                s.Status == Status.ConditionallyApproved ||
                                                                s.Status == Status.Rejected ||
                                                                s.Status == Status.MissingInformation).ToList();
            var grouppedResult = submissions.GroupBy(x => x.Status);

            foreach(var g in grouppedResult)
            {
                if (g.Key == Status.Approved)
                    results.Approved = g.Count();
                else if (g.Key == Status.ConditionallyApproved)
                    results.ConditionallyApproved = g.Count();
                else if (g.Key == Status.Rejected)
                    results.Rejected = g.Count();
                else if (g.Key == Status.MissingInformation)
                    results.MissingInformation = g.Count();
            }

            return results;
        }

        private ResponseDays GetTurnaroundTime()
        {
            Dictionary<Status, Tuple<TimeSpan, int>> times = new Dictionary<Status, Tuple<TimeSpan, int>>
            {
                {Status.Approved, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.Rejected, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.MissingInformation, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
            };
            List<Submission> submissions = _applicationDbContext.Submissions
                                                                .Where(s => s.Status == Status.Approved ||
                                                                s.Status == Status.ConditionallyApproved ||
                                                                s.Status == Status.Rejected ||
                                                                s.Status == Status.MissingInformation).ToList();

            foreach (var sub in submissions)
            {
                if (sub.Status == Status.ConditionallyApproved)
                {
                    sub.Status = Status.Approved;
                }
                TimeSpan elapsed = sub.LastModified.Subtract(sub.SubmissionDate);

                Tuple<TimeSpan, int> time = times[sub.Status];

                times[sub.Status] = new Tuple<TimeSpan, int>(time.Item1.Add(elapsed), time.Item2 + 1);

                
            }


            ResponseDays results = new ResponseDays();

            foreach(var status in times.Keys)
            {
                var tuple = times[status];
                if (tuple.Item2 == 0)
                    continue;

                TimeSpan total = new TimeSpan(tuple.Item1.Ticks / tuple.Item2);

                if (status == Status.Approved)
                    results.Approved = total.Days;
                if (status == Status.Rejected)
                    results.Rejected = total.Days;
                if (status == Status.MissingInformation)
                    results.MissingInformation = total.Days;
            }


            return results;
        }

        private DaysByCategory TimeByStage()
        {
            Dictionary<Status, Tuple<TimeSpan, int>> times = new Dictionary<Status, Tuple<TimeSpan, int>>
            {
                {Status.Submitted, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.ARBIncoming, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.UnderReview, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.ARBFinal, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.ReviewComplete, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },
                {Status.PrepApproval, new Tuple<TimeSpan, int>(TimeSpan.Zero, 0) },                
            };
            var states = _applicationDbContext.StateChanges.Where(s => s.EndTime != DateTime.MinValue).ToList();
            
            foreach (var change in states)
            {
                if (change.State == Status.PrepConditionalApproval)
                {
                    change.State = Status.PrepApproval;
                }
                TimeSpan elapsed = change.EndTime.Subtract(change.StartTime);

                if(!times.Keys.Any(k => k == change.State))
                {
                    continue;
                }

                Tuple<TimeSpan, int> time = times[change.State];

                times[change.State] = new Tuple<TimeSpan, int>(time.Item1.Add(elapsed), time.Item2 + 1);
            }


            DaysByCategory results = new DaysByCategory();

            foreach (var status in times.Keys)
            {
                var tuple = times[status];
                if (tuple.Item2 == 0)
                    continue;

                TimeSpan total = new TimeSpan(tuple.Item1.Ticks / tuple.Item2);

                if (status == Status.Submitted)
                    results.CheckCompleteness = total.Days;
                if (status == Status.ARBIncoming)
                    results.ARBCheck = total.Days;
                if (status == Status.UnderReview)
                    results.UnderReview = total.Days;
                if (status == Status.ARBFinal)
                    results.TallyVotes = total.Days;
                if (status == Status.ReviewComplete)
                    results.HOALiason = total.Days;
                if (status == Status.PrepApproval)
                    results.PrepApproval = total.Days;
            }


            return results;
        }
    }

    public class StatsModel
    {
        public List<Tuple<string, int>> ByMonth { get; set; }
        public ResponseCount ResponseCounts { get; set; }
        public ResponseDays ElapsedTime { get; set; }        
        public int TotalSubmissions { get; set; }
        public DaysByCategory DaysByCategory { get; set; }
    }

    public class ResponseCount
    {
        public int Rejected { get; set; }
        public int Approved { get; set; }
        public int ConditionallyApproved { get; set; }
        public int MissingInformation { get; set; }
    }

    public class ResponseDays
    {
        public int Rejected { get; set; }
        public int Approved { get; set; }
        public int MissingInformation { get; set; }
    }

    public class DaysByCategory
    {
        public int CheckCompleteness { get; set; }
        public int ARBCheck { get; set; }
        public int UnderReview { get; set; }
        public int TallyVotes { get; set; }
        public int HOALiason { get; set; }
        public int PrepApproval { get; set; }
    }

}
