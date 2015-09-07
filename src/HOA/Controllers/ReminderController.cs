using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Identity;
using HOA.Model;
using Microsoft.AspNet.Authorization;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class ReminderController : Controller
    {
        private static readonly TimeSpan ReminderTime_Submitted = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_ARBIncoming = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_Review = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_ARBPost = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_Final = new TimeSpan(3, 0, 0, 0); //3 days

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;

        public ReminderController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
        }
        
        [AllowAnonymous]
        public IActionResult Process()
        {
            var allOpen = _applicationDbContext.Submissions.Where(s => s.Status != Status.Approved && s.Status != Status.Rejected).ToList();

            foreach(var submission in allOpen)
            {
                TimeSpan timeAllowance = TimeSpan.MaxValue;
                switch (submission.Status)
                {
                    case Status.Submitted:
                        timeAllowance = ReminderTime_Submitted;
                        break;
                    case Status.ARBIncoming:
                        timeAllowance = ReminderTime_ARBIncoming;
                        break;
                    case Status.UnderReview:
                        timeAllowance = ReminderTime_Review;
                        break;
                    case Status.ARBFinal:
                        timeAllowance = ReminderTime_ARBPost;
                        break;
                    case Status.ReviewComplete:
                        timeAllowance = ReminderTime_Final;
                        break;
                }

                if(DateTime.Now > submission.LastModified.Add(timeAllowance))
                {
                    //overdue, send email
                }
            }


            return View();
        }
    }
}
