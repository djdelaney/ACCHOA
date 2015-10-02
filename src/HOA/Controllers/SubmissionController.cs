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
    public class SubmissionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;
        private RoleManager<IdentityRole> _roleManager;

        public SubmissionController(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, IEmailSender emailSender, RoleManager<IdentityRole> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _email = emailSender;
            _roleManager = roleManager;
        }

        private int GetReviewerCount()
        {
            var role = _applicationDbContext.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(RoleNames.BoardMember));
            List<string> userIds = role.Users.Select(u => u.UserId).ToList();
            var users = _applicationDbContext.Users.Where(u => userIds.Contains(u.Id) && u.Enabled);
            return users.Count();
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

        [Route("Submission/List/{filter?}")]
        public IActionResult List(string filter)
        {
            IQueryable<Submission> subs = _applicationDbContext.Submissions;

            if(string.IsNullOrEmpty(filter))
            {
                filter = "Incoming";
            }

            if (filter.Equals("Incoming"))
            {
                subs = subs.Where(s => s.Status != Status.Approved && s.Status != Status.Rejected && s.Status != Status.ConditionallyApproved && s.Status != Status.MissingInformation);

                if (User.IsInRole(RoleNames.Administrator))
                {
                }
                else if (User.IsInRole(RoleNames.CommunityManager))
                {
                    subs = subs.Where(s => s.Status == Status.Submitted);
                }
                else if (User.IsInRole(RoleNames.BoardChairman))
                {
                    subs = subs.Where(s => s.Status == Status.ARBIncoming);
                }
                else if (User.IsInRole(RoleNames.BoardMember))
                {
                    subs = subs.Where(s => s.Status == Status.UnderReview);
                }
                else if (User.IsInRole(RoleNames.BoardChairman))
                {
                    subs = subs.Where(s => s.Status == Status.ARBFinal);
                }
                else if (User.IsInRole(RoleNames.HOALiaison))
                {
                    subs = subs.Where(s => s.Status == Status.ReviewComplete);
                }
            }
            else if (filter.Equals("Approved"))
            {
                subs = subs.Where(s => s.Status == Status.Approved || s.Status == Status.ConditionallyApproved);
            }
            else if (filter.Equals("Rejected"))
            {
                subs = subs.Where(s => s.Status == Status.Rejected || s.Status == Status.MissingInformation);
            }
            //deault to all

            var viewModel = new ViewSubmissionsViewModel()
            {
                Filter = filter,
                Submissions = subs.Include(s => s.Audits).OrderBy(s => s.LastModified).ToList()
            };

            return View(viewModel);
        }

        public ActionResult View(int? id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .ThenInclude(r => r.Reviewer)
                .Include(s => s.Audits)
                .Include(s => s.Files)
                .FirstOrDefault(s => s.Id == id);
            if(submission == null)
                return HttpNotFound("Submission not found");

            submission.Audits = submission.Audits.OrderByDescending(a => a.DateTime).ToList();
            
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
                    Code = DBUtil.GenerateUniqueCode(_applicationDbContext),
                    Files = new List<File>(),
                    Revision = 1
                };

                foreach(var fileContent in model.Files)
                {
                    var chunks = fileContent.ContentDisposition.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var nameChunk = chunks.FirstOrDefault(c => c.Contains("filename"));
                    var fileName = nameChunk.Split('=')[1].Trim(new char[] { '"' });

                    var file = new File
                    {
                        Name = fileName,
                        BlobName = "TODO"
                    };

                    sub.Files.Add(file);
                    _applicationDbContext.Files.Add(file);
                }

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
        [Authorize(Roles = RoleNames.CommunityManager)]
        [Authorize(Roles = RoleNames.BoardChairman)]
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
                    submission.Status = Status.MissingInformation;
                
                var user = await _userManager.FindByIdAsync(User.GetUserId());                
                string action = string.Format("Marked {0}. Comments: {1}", model.Approve ? "complete" : "missing information", model.Comments);
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

            ReviewSubmissionViewModel model = new ReviewSubmissionViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.BoardMember)]
        public async Task<IActionResult> Review(ReviewSubmissionViewModel model)
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
                    Status = (ReviewStatus)Enum.Parse(typeof(ReviewStatus), model.Status),
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

        [HttpGet]
        public IActionResult TallyVotes(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews).ThenInclude(r => r.Reviewer).FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            ReviewSubmissionViewModel model = new ReviewSubmissionViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.BoardChairman)]
        public async Task<IActionResult> TallyVotes(ReviewSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return HttpNotFound("Submission not found");

                var status = (ReviewStatus)Enum.Parse(typeof(ReviewStatus), model.Status);

                if (status == ReviewStatus.Approved || status == ReviewStatus.ConditionallyApproved)
                {
                    submission.Status = Status.ReviewComplete;
                }
                else if (status == ReviewStatus.MissingInformation)
                {
                    submission.Status = Status.MissingInformation;
                }
                else
                {
                    submission.Status = Status.Rejected;
                }

                var user = await _userManager.FindByIdAsync(User.GetUserId());
                string action = string.Format("{0}. Comments: {1}", model.Status, model.Comments);
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
        [Authorize(Roles = RoleNames.HOALiaison)]
        public IActionResult FinalCheck(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            ReviewSubmissionViewModel model = new ReviewSubmissionViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalCheck(ReviewSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return HttpNotFound("Submission not found");

                var status = (Status)Enum.Parse(typeof(Status), model.Status);

                if (status == Status.Approved)
                {
                    submission.Status = Status.Approved;
                }
                else if (status == Status.ConditionallyApproved)
                {
                    submission.Status = Status.ConditionallyApproved;
                }
                else if (status == Status.MissingInformation)
                {
                    submission.Status = Status.MissingInformation;
                }
                else
                {
                    submission.Status = Status.Rejected;
                }

                var user = await _userManager.FindByIdAsync(User.GetUserId());
                string action = string.Format("Marked {0}. Comments: {1}", model.Status, model.Comments);
                AddHistoryEntry(submission, user.FullName, action);

                submission.LastModified = DateTime.Now;
                _applicationDbContext.SaveChanges();
                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }
    }
}
