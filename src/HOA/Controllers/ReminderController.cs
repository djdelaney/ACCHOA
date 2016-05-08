using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Identity;
using HOA.Model;
using Microsoft.AspNet.Authorization;
using HOA.Util;
using HOA.Services;
using Microsoft.Data.Entity;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class ReminderController : Controller
    {
        private static readonly TimeSpan ReminderTime_Submitted = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_ARBIncoming = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_Review = new TimeSpan(5, 0, 0, 0); //5 days
        private static readonly TimeSpan ReminderTime_ARBPost = new TimeSpan(3, 0, 0, 0); //3 days
        private static readonly TimeSpan ReminderTime_Final = new TimeSpan(3, 0, 0, 0); //3 days

        private static readonly TimeSpan Quarum_Final = new TimeSpan(3, 0, 0, 0); //3 days

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;

        public ReminderController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext applicationDbContext,
            IEmailSender mail)
        {
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
            _email = mail;
        }
        
        [AllowAnonymous]
        public IActionResult Process()
        {
            Dictionary<string, List<Submission>> toNotify = new Dictionary<string, List<Submission>>();

            var allOpen = _applicationDbContext.Submissions.Where(
                s => s.Status != Status.Approved && s.Status != Status.Rejected && s.Status != Status.ConditionallyApproved && s.Status != Status.MissingInformation
                ).ToList();

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

                if(DateTime.Now > submission.StatusChangeTime.Add(timeAllowance))
                {
                    //Group notifications together
                    List<string> emailsToNotify = EmailHelper.GetOverdueRecipients(_applicationDbContext, submission);
                    foreach (var email in emailsToNotify)
                    {
                        if (!toNotify.ContainsKey(email))
                            toNotify[email] = new List<Submission>();

                        toNotify[email].Add(submission);
                    }
                }
            }

            foreach (KeyValuePair<string, List<Submission>> entry in toNotify)
            {
                EmailHelper.NotifySubmissonsOverdue(entry.Key, entry.Value, _email);
            }

            return Content("Processed");
        }

        [AllowAnonymous]
        public IActionResult CheckQuarum()
        {
            int totalReviewers = SubmissionController.GetReviewerCount(_applicationDbContext);
            int quarum = (int)Math.Ceiling((double)totalReviewers / (double)2);

            var allOpen = _applicationDbContext.Submissions.Where(s => s.Status == Status.UnderReview).Include(s => s.Audits).Include(s => s.Reviews).ToList();
            foreach (Submission submission in allOpen)
            {
                if (submission.Reviews.Count >= quarum && DateTime.Now > submission.StatusChangeTime.Add(Quarum_Final))
                {
                    submission.Status = Status.ARBFinal;
                    submission.LastModified = DateTime.Now;
                    submission.StatusChangeTime = DateTime.Now;


                    if (submission.Audits == null)
                        submission.Audits = new List<History>();

                    var history = new History
                    {
                        User = "System",
                        DateTime = DateTime.Now,
                        Action = "Quarum reached after delay, tallying votes.",
                        Submission = submission
                    };
                    submission.Audits.Add(history);
                    _applicationDbContext.Histories.Add(history);
                    _applicationDbContext.SaveChanges();
                    
                    EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);
                }
            }

            return Content("Processed");
        }
    }
}
