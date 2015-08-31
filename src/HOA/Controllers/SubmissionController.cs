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
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if(submission == null)
                return HttpNotFound("Submission not found");

            var model = new ViewSubmissionViewModel()
            {
                Submission = submission
            };

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
                    Status = Status.Submitted
                };

                _applicationDbContext.Submissions.Add(sub);
                _applicationDbContext.SaveChanges();
                
                Console.WriteLine("Create");
                return Content("Submission ID: " + sub.Id);
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
                var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
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

                var history = new History
                {
                    User = user,
                    DateTime = DateTime.Now,
                    Action = model.Comments,
                    Submission = submission
                };
                if (submission.Audits == null)
                    submission.Audits = new List<History>();
                submission.Audits.Add(history);
                _applicationDbContext.Histories.Add(history);
                _applicationDbContext.SaveChanges();
                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }


        [HttpGet]
        public IActionResult ApproveReject(int id)
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
        public async Task<IActionResult> ApproveReject(ApproveRejectViewModel model)
        {
            if (ModelState.IsValid)
            {
                /*
                _applicationDbContext.Submissions.Add(sub);
                _applicationDbContext.SaveChanges();

                Console.WriteLine("Create");
                return Content("Submission ID: " + sub.Id);*/
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }
    }
}
