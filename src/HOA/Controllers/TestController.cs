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
using Microsoft.AspNetCore.Http;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [RequireHttps]
    [Authorize]
    public class TestController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;
        private readonly IFileStore _storage;
        private IHostingEnvironment _env;
        private RoleManager<IdentityRole> _roleManager;

        private Random _rand;

        public TestController(ApplicationDbContext applicationDbContext,
                            UserManager<ApplicationUser> userManager,
                            IEmailSender emailSender,
                            IFileStore fileStore,
                            RoleManager<IdentityRole> roleManager,
                            IHostingEnvironment env)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _email = emailSender;
            _roleManager = roleManager;
            _storage = fileStore;
            _rand = new Random();
            _env = env;
        }

        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult CreateSample()
        {
            CreateTestViewModel model = new CreateTestViewModel()
            {
                Count = 1,
                Type = Status.CommunityMgrReview.ToString()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult CreateSample(CreateTestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var status = (Status)Enum.Parse(typeof(Status), model.Type);

                for (int x = 0; x < model.Count; x++)
                {
                    var sub = CreateSubmission();
                    SetStatus(sub, status);
                }
                _applicationDbContext.SaveChanges();

                return RedirectToAction("List", "Submission");
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult DeleteAll()
        {
            if (!_env.IsDevelopment())
                throw new Exception("Hell no.");

            DeleteAllViewModel model = new DeleteAllViewModel()
            {
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Administrator)]
        public async Task<IActionResult> DeleteAll(DeleteAllViewModel model)
        {
            if (!_env.IsDevelopment())
                throw new Exception("Hell no.");

            if (ModelState.IsValid)
            {
                var files = _applicationDbContext.Files.ToList();
                foreach (var f in files)
                {
                    if (!f.BlobName.Equals("NONE"))
                        await _storage.DeleteFile(f.BlobName);
                }

                foreach (var entity in _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments))
                    _applicationDbContext.Submissions.Remove(entity);
                foreach (var entity in _applicationDbContext.Comments)
                    _applicationDbContext.Comments.Remove(entity);
                foreach (var entity in _applicationDbContext.Histories)
                    _applicationDbContext.Histories.Remove(entity);
                foreach (var entity in _applicationDbContext.Reviews)
                    _applicationDbContext.Reviews.Remove(entity);
                foreach (var entity in _applicationDbContext.Files)
                    _applicationDbContext.Files.Remove(entity);
                foreach (var entity in _applicationDbContext.Responses)
                    _applicationDbContext.Responses.Remove(entity);

                _applicationDbContext.SaveChanges();

                return RedirectToAction("List", "Submission");
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult CreateRandom()
        {
            CreateRandomViewModel model = new CreateRandomViewModel()
            {
                Count = 10
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Administrator)]
        public IActionResult CreateRandom(CreateRandomViewModel model)
        {
            if (ModelState.IsValid)
            {
                for (int x = 0; x < model.Count; x++)
                {
                    var sub = CreateSubmission();
                    Status[] statuses =
                    {
                        Status.CommunityMgrReview,
                        Status.ARBChairReview,
                        Status.CommitteeReview,
                        Status.ARBTallyVotes,
                        Status.HOALiasonReview,
                        Status.FinalResponse,

                        Status.CommunityMgrReturn,

                        Status.HOALiasonInput,

                        Status.Rejected,
                        Status.MissingInformation,
                        Status.Approved,
                        Status.ConditionallyApproved,
                        Status.Retracted,
                    };
                    var status = statuses[_rand.Next(statuses.Length)];
                    SetStatus(sub, status);
                    _applicationDbContext.SaveChanges();
                }

                return RedirectToAction("List", "Submission");
            }
            return View(model);
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
                Status = Status.CommunityMgrReview,
                StatusChangeTime = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                SubmissionDate = DateTime.UtcNow.AddHours(-1),
                Code = DBUtil.GenerateUniqueCode(_applicationDbContext),
                Reviews = new List<Review>(),
                Audits = new List<History>(),
                Responses = new List<Response>(),
                Files = new List<File>(),
                StateHistory = new List<StateChange>(),
                Comments = new List<Comment>(),
                Revision = 1,
                PrecedentSetting = false
            };

            var file = new File
            {
                Name = "Application.pdf",
                BlobName = "NONE"
            };

            sub.Files.Add(file);
            _applicationDbContext.Submissions.Add(sub);
            _applicationDbContext.Files.Add(file);


            return sub;
        }

        private void SetStatus(Submission sub, Status status)
        {

            AddHistoryEntry(sub, "Test user", string.Format("Moving to {0}", status));

            if (status == Status.ARBChairReview)
            {
                sub.Status = Status.ARBChairReview;
            }
            else if (status == Status.CommitteeReview)
            {
                SetStatus(sub, Status.ARBChairReview);
                sub.Status = Status.CommitteeReview;
            }
            else if (status == Status.ARBTallyVotes)
            {
                SetStatus(sub, Status.CommitteeReview);
                sub.Status = Status.ARBTallyVotes;

                var user = _userManager.FindByIdAsync(_userManager.GetUserId(User)).Result;
                var review = new Review
                {
                    Reviewer = user,
                    Status = ReviewStatus.Approved,
                    Created = DateTime.UtcNow,
                    Comments = "BLAH",
                    Submission = sub,
                    SubmissionRevision = sub.Revision
                };

                sub.Reviews = new List<Review>();
                sub.Reviews.Add(review);
                _applicationDbContext.Reviews.Add(review);
            }
            else if (status == Status.HOALiasonReview)
            {
                SetStatus(sub, Status.ARBTallyVotes);
                sub.Status = Status.HOALiasonReview;
            }
            else if (status == Status.FinalResponse)
            {
                SetStatus(sub, Status.HOALiasonReview);
                sub.ReturnStatus = ReturnStatus.Approved;
                sub.Status = Status.FinalResponse;
            }
            else if (status == Status.CommunityMgrReturn)
            {
                SetStatus(sub, Status.HOALiasonReview);
                sub.ReturnStatus = ReturnStatus.MissingInformation;
                sub.Status = Status.CommunityMgrReturn;
            }
            else if (status == Status.Rejected)
            {
                SetStatus(sub, Status.CommunityMgrReturn);
                sub.Status = Status.Rejected;
            }
            else if (status == Status.Approved)
            {
                SetStatus(sub, Status.CommunityMgrReturn);
                sub.Status = Status.Approved;
            }
            else if (status == Status.ConditionallyApproved)
            {
                SetStatus(sub, Status.FinalResponse);
                sub.Status = Status.ConditionallyApproved;
            }
            else if (status == Status.MissingInformation)
            {
                SetStatus(sub, Status.CommunityMgrReturn);
                sub.Status = Status.MissingInformation;
            }

            sub.LastModified = DateTime.UtcNow;
        }

        private void AddHistoryEntry(Submission s, string user, string action)
        {
            if (s.Audits == null)
                s.Audits = new List<History>();

            var history = new History
            {
                User = user,
                DateTime = DateTime.UtcNow,
                Action = action,
                Submission = s
            };
            s.Audits.Add(history);
            _applicationDbContext.Histories.Add(history);
        }
    }
}
