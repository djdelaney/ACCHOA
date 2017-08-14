using System;
using System.Collections.Generic;
using System.Linq;
using HOA.Model;
using HOA.Util;
using HOA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class ReminderController : Controller
    {
        private static readonly TimeSpan ReminderTime_General = new TimeSpan(3, 0, 0, 0); //3 days

        private static readonly TimeSpan Quorum_Final = new TimeSpan(5, 0, 0, 0); //5 days

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

            var allOpen = _applicationDbContext.Submissions.Where(s => 
                    s.Status != Status.Approved && 
                    s.Status != Status.Rejected &&
                    s.Status != Status.ConditionallyApproved &&
                    s.Status != Status.MissingInformation &&
                    s.Status != Status.Retracted
                ).ToList();

            foreach(var submission in allOpen)
            {
                TimeSpan timeAllowance = ReminderTime_General;
                /*switch (submission.Status)
                {
                    case Status.CommitteeReview:
                        timeAllowance = ReminderTime_General;
                        break;
                    default:
                        continue;
                }*/

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
        public IActionResult CheckQuorum()
        {
            int totalReviewersLandscaping = SubmissionController.GetReviewerCount(_applicationDbContext, true);
            int quorumWithLandscaping = (int)Math.Ceiling((double)totalReviewersLandscaping / (double)2);

            int totalReviewersNoLandscaping = SubmissionController.GetReviewerCount(_applicationDbContext, false);
            int quorumWithoutLandscaping = (int)Math.Ceiling((double)totalReviewersNoLandscaping / (double)2);

            var allOpen = _applicationDbContext.Submissions.Where(s => s.Status == Status.CommitteeReview).Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments).ToList();
            foreach (Submission submission in allOpen)
            {
                int quorum = submission.LandscapingRelated ? quorumWithLandscaping : quorumWithoutLandscaping;

                if (submission.Reviews.Count >= quorum && DateTime.Now > submission.StatusChangeTime.Add(Quorum_Final))
                {
                    submission.Status = Status.ARBTallyVotes;
                    submission.LastModified = DateTime.UtcNow;
                    submission.StatusChangeTime = DateTime.UtcNow;


                    if (submission.Audits == null)
                        submission.Audits = new List<History>();

                    var history = new History
                    {
                        User = "System",
                        DateTime = DateTime.UtcNow,
                        Action = "Quorum reached after delay, tallying votes.",
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
