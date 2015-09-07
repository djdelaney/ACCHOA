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

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class SubmissionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;

        public SubmissionController(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
        }

        private int GetReviewerCount()
        {
            var role = _applicationDbContext.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(RoleNames.BoardMember));
            return role.Users.Count;            
        }

        private string GenerateUniqueCode()
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
            while (_applicationDbContext.Submissions.Any(s => s.Code.Equals(code)));
            return code;
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
        
        //[Authorize(Roles = "CommunityManager")]
        public IActionResult List()
        {
            var viewModel = new ViewSubmissionsViewModel();

            if(User.IsInRole(RoleNames.CommunityManager))
            {
                viewModel.NewSubmissions = _applicationDbContext.Submissions.Where(s => s.Status == Status.Submitted).Include(s => s.Audits).ToList();
            }

            if (User.IsInRole(RoleNames.BoardChairman))
            {
                viewModel.ARBIncoming = _applicationDbContext.Submissions.Where(s => s.Status == Status.ARBIncoming).Include(s => s.Audits).ToList();
            }

            if (User.IsInRole(RoleNames.BoardMember))
            {
                viewModel.ForReview = _applicationDbContext.Submissions.Where(s => s.Status == Status.UnderReview).Include(s => s.Audits).ToList();
            }

            if (User.IsInRole(RoleNames.BoardChairman))
            {
                viewModel.ARBFinal = _applicationDbContext.Submissions.Where(s => s.Status == Status.ARBFinal).Include(s => s.Audits).ToList();
            }

            if (User.IsInRole(RoleNames.HOALiaison))
            {
                viewModel.FinalApproval = _applicationDbContext.Submissions.Where(s => s.Status == Status.ReviewComplete).Include(s => s.Audits).ToList();
            }

            return View(viewModel);
        }

        public ActionResult View(int? id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .ThenInclude(r => r.Reviewer)
                .Include(s => s.Audits)
                .FirstOrDefault(s => s.Id == id);
            if(submission == null)
                return HttpNotFound("Submission not found");

            submission.Audits = submission.Audits.OrderBy(a => a.DateTime).ToList();
            
            var model = new ViewSubmissionViewModel()
            {
                Submission = submission,
                ReviewerCount = GetReviewerCount(),
                Reviewed = submission.Reviews.Any(r => r.Reviewer.Id == User.GetUserId())
            };

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult LookupStatus()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LookupStatus(StatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions
                    .Include(s => s.Audits)
                    .FirstOrDefault(s => s.Code.Equals(model.Code));

                submission.Audits = submission.Audits.OrderBy(a => a.DateTime).ToList();

                if (submission == null)
                    return View("StatusNotFound");

                return View("ViewStatus", submission);
            }

            //form not complete
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateSubmissionViewModel model)
        {
            //http://stackoverflow.com/questions/15680629/mvc-4-razor-file-upload

            if (ModelState.IsValid)
            {

                var sub = new Submission()
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    Email = model.Email,
                    Description = model.Description,
                    Status = Status.Submitted,
                    LastModified = DateTime.Now,
                    Code = GenerateUniqueCode()
                };

                AddHistoryEntry(sub, model.FirstName + " " + model.LastName, "Submitted");

                _applicationDbContext.Submissions.Add(sub);
                _applicationDbContext.SaveChanges();

                return View("PostCreate", sub);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        
        [HttpGet]
        [Authorize(Roles = RoleNames.CommunityManager)]
        [Authorize(Roles = RoleNames.BoardChairman)]
        public IActionResult CheckCompleteness(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            if (submission.Status == Status.Submitted && !User.IsInRole(RoleNames.CommunityManager))
                return HttpNotFound("Not authorized");
            if (submission.Status == Status.ARBIncoming && !User.IsInRole(RoleNames.BoardChairman))
                return HttpNotFound("Not authorized");

            ApproveRejectViewModel model = new ApproveRejectViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckCompleteness(ApproveRejectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return HttpNotFound("Submission not found");

                if (model.Approve)
                {
                    if (submission.Status == Status.Submitted)
                    {
                        submission.Status = Status.ARBIncoming;
                    }
                    else
                    {
                        submission.Status = Status.UnderReview;
                    }
                }
                else
                    submission.Status = Status.Rejected;
                
                var user = await _userManager.FindByIdAsync(User.GetUserId());                
                string action = string.Format("marked{0} complete- {1}", model.Approve ? "" : " not", model.Comments);
                AddHistoryEntry(submission, user.FullName, action);

                submission.LastModified = DateTime.Now;
                _applicationDbContext.SaveChanges();
                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }


        [HttpGet]
        public IActionResult Review(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            ApproveRejectViewModel model = new ApproveRejectViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(ApproveRejectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.Reviews)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return HttpNotFound("Submission not found");

                var user = await _userManager.FindByIdAsync(User.GetUserId());

                var review = new Review
                {
                    Reviewer = user,
                    Approved = model.Approve,
                    Created = DateTime.Now,
                    Comments = model.Comments,
                    Submission = submission
                };

                if (submission.Reviews == null)
                    submission.Reviews = new List<Review>();
                submission.Reviews.Add(review);

                AddHistoryEntry(submission, user.FullName, "Submitted review");

                _applicationDbContext.Reviews.Add(review);

                //Final review!
                if (submission.Reviews.Count == GetReviewerCount())
                {
                    submission.Status = Status.ARBFinal;
                    submission.LastModified = DateTime.Now;
                    AddHistoryEntry(submission, "System", "All reviews in, sent to chairman");
                }
                
                _applicationDbContext.SaveChanges();

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }
    }
}
