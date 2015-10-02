using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using HOA.Model;
using HOA.Model.ViewModel;
using Microsoft.Data.Entity;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using HOA.Services;
using Microsoft.AspNet.Identity.EntityFramework;
using HOA.Util;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class TestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;
        private RoleManager<IdentityRole> _roleManager;

        private Random _rand;

        public TestController(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, IEmailSender emailSender, RoleManager<IdentityRole> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _email = emailSender;
            _roleManager = roleManager;
            _rand = new Random();
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Index()
        {
            for (int x = 0; x < 10; x++)
            {
                var sub = CreateSubmission();

                Status[] statuses = { Status.ARBIncoming, Status.UnderReview, Status.ARBFinal, Status.ReviewComplete, Status.Approved, Status.Rejected };

                var status = statuses[_rand.Next(statuses.Length)];
                SetStatus(sub, status);
            }

            _applicationDbContext.SaveChanges();
            //return Content("Created");
            return RedirectToAction("List", "Submission");
        }

        private Submission CreateSubmission()
        {
            string[] fnames = { "Dan", "Ali", "Sarah", "Alan", "Martha" };
            string[] lnames = { "Delaney", "Kolakowski", "KR", "Doe", "Smith" };
            string[] streets = { "Sills Ln", "Meadowlake Dr", "Brandywine Dr", "Lea Ct" };
            string[] objects = { "Deck", "Porch", "Patio", "Shed" };

            var name = fnames[_rand.Next(fnames.Length)];
            var sub = new Submission()
            {
                FirstName = name,
                LastName = lnames[_rand.Next(lnames.Length)],
                Address = string.Format("{0} {1}", _rand.Next(200), streets[_rand.Next(streets.Length)]),
                Email = string.Format("{0}@mailinator.com", name),
                Description = string.Format("Build a {0}", objects[_rand.Next(objects.Length)]),
                Status = Status.Submitted,
                LastModified = DateTime.Now,
                Code = DBUtil.GenerateUniqueCode(_applicationDbContext),
                Files = new List<File>(),
                Audits = new List<History>(),
                Revision = 1
            };

            var file = new File
            {
                Name = "Application.pdf",
                BlobName = "TODO"
            };

            sub.Files.Add(file);
            _applicationDbContext.Submissions.Add(sub);
            _applicationDbContext.Files.Add(file);
            

            return sub;
        }

        private void SetStatus(Submission sub, Status status)
        {
            
            AddHistoryEntry(sub, "Test user", string.Format("Moving to {0}", status));

            if (status == Status.ARBIncoming)
            {
                sub.Status = Status.ARBIncoming;
            }
            else if (status == Status.UnderReview)
            {
                SetStatus(sub, Status.ARBIncoming);
                sub.Status = Status.UnderReview;
            }
            else if (status == Status.ARBFinal)
            {
                SetStatus(sub, Status.UnderReview);
                sub.Status = Status.ARBFinal;

                var user = _userManager.FindByIdAsync(User.GetUserId()).Result;
                var review = new Review
                {
                    Reviewer = user,
                    Status = ReviewStatus.Approved,
                    Created = DateTime.Now,
                    Comments = "BLAH",
                    Submission = sub
                };

                sub.Reviews = new List<Review>();
                sub.Reviews.Add(review);
                _applicationDbContext.Reviews.Add(review);
            }
            else if (status == Status.ReviewComplete)
            {
                SetStatus(sub, Status.ARBFinal);
                sub.Status = Status.ReviewComplete;
            }
            else if (status == Status.Rejected)
            {
                SetStatus(sub, Status.ReviewComplete);
                sub.Status = Status.Rejected;
            }
            else if (status == Status.Approved)
            {
                SetStatus(sub, Status.ReviewComplete);
                sub.Status = Status.Approved;
            }

            sub.LastModified = DateTime.Now;
        }

        private void AddHistoryEntry(Submission s, string user, string action)
        {
            if (s.Audits == null)
                s.Audits = new List<History>();

            var history = new History
            {
                User = user,
                DateTime = DateTime.Now,
                Action = action,
                Submission = s
            };
            s.Audits.Add(history);
            _applicationDbContext.Histories.Add(history);
        }
    }
}
