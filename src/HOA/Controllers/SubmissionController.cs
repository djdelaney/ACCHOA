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
using Microsoft.AspNet.Http;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [Authorize]
    public class SubmissionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;
        private readonly IFileStore _storage;
        private RoleManager<IdentityRole> _roleManager;

        private const int maxSizeBytes = 10 * 1024 * 1024; //10MB

        public SubmissionController(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, IEmailSender emailSender, IFileStore store, RoleManager<IdentityRole> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _email = emailSender;
            _roleManager = roleManager;
            _storage = store;
        }

        public static int GetReviewerCount(ApplicationDbContext context)
        {
            var role = context.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(RoleNames.BoardMember));
            List<string> userIds = role.Users.Select(u => u.UserId).ToList();
            var users = context.Users.Where(u => userIds.Contains(u.Id) && u.Enabled);
            return users.Count();
        }

        public void AddStateSwitch(Submission s)
        {
            if (s.StateHistory == null)
                s.StateHistory = new List<StateChange>();

            StateChange existing = s.StateHistory.FirstOrDefault(h => h.EndTime.Equals(DateTime.MinValue));
            if(existing != null)
            {
                existing.EndTime = DateTime.Now;
            }

            //Dont log duration for final statuses
            if(s.Status == Status.Approved || s.Status == Status.ConditionallyApproved)
            {
                return;
            }

            var change = new StateChange
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.MinValue,
                Submission = s,
                State = s.Status
            };
            s.StateHistory.Add(change);
            _applicationDbContext.StateChanges.Add(change);
        }

        public void AddHistoryEntry(Submission s, string user, string action)
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

        [HttpGet]
        [Authorize(Roles = "CommunityManager")]
        public IActionResult FinalResponse(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            FinalResponseViewModel model = new FinalResponseViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.CommunityManager)]
        public async Task<IActionResult> FinalResponse(FinalResponseViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.Reviews).Include(s => s.StateHistory)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return HttpNotFound("Submission not found");

                var user = await _userManager.FindByIdAsync(User.GetUserId());

                if (submission.Status == Status.PrepApproval)
                    submission.Status = Status.Approved;
                else
                    submission.Status = Status.ConditionallyApproved;

                AddStateSwitch(submission);

                //Any final comments?
                if (!string.IsNullOrEmpty(model.Comments))
                {
                    var response = new Response
                    {
                        Created = DateTime.Now,
                        Comments = model.Comments,
                        Submission = submission
                    };
                    if (submission.Responses == null)
                        submission.Responses = new List<Response>();
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }

                AddHistoryEntry(submission, user.FullName, "Sent final response");

                _applicationDbContext.SaveChanges();

                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }

        [Route("Submission/List/{filter?}")]
        public IActionResult List(string filter)
        {
            IQueryable<Submission> subs = _applicationDbContext.Submissions;

            if(string.IsNullOrEmpty(filter))
            {
                filter = "Todo";
            }

            if (filter.Equals("Todo"))
            {
                subs = subs.Where(s => s.Status != Status.Approved && s.Status != Status.Rejected && s.Status != Status.ConditionallyApproved && s.Status != Status.MissingInformation && s.Status != Status.Retracted);

                if (User.IsInRole(RoleNames.Administrator))
                {
                }
                else if (User.IsInRole(RoleNames.CommunityManager))
                {
                    subs = subs.Where(s => s.Status == Status.Submitted || s.Status == Status.PrepApproval || s.Status == Status.PrepConditionalApproval);
                }
                else if (User.IsInRole(RoleNames.BoardChairman))
                {
                    subs = subs.Where(s => s.Status == Status.ARBIncoming || s.Status == Status.ARBFinal);
                }
                else if (User.IsInRole(RoleNames.BoardMember))
                {
                    subs = subs.Where(s => s.Status == Status.UnderReview);
                }
                else if (User.IsInRole(RoleNames.HOALiaison))
                {
                    subs = subs.Where(s => s.Status == Status.ReviewComplete);
                }
            }
            else if (filter.Equals("Open"))
            {
                subs = subs.Where(s => s.Status != Status.Rejected &&
                                s.Status != Status.MissingInformation &&
                                s.Status != Status.Approved &&
                                s.Status != Status.ConditionallyApproved &&
                                s.Status != Status.Retracted);
            }
            else if (filter.Equals("Recent"))
            {
                DateTime oneMonthAgo = DateTime.Now.AddDays(-30);
                subs = subs.Where(s => (s.Status != Status.Rejected &&
                                s.Status != Status.MissingInformation &&
                                s.Status != Status.Approved &&
                                s.Status != Status.ConditionallyApproved &&
                                s.Status != Status.Retracted) &&
                                s.LastModified > oneMonthAgo);
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
                ReviewerCount = GetReviewerCount(_applicationDbContext),
                CurrentReviewCount = submission.Reviews.Count(r => r.SubmissionRevision == submission.Revision),
                Reviewed = submission.Reviews.Any(r => r.Reviewer.Id == User.GetUserId() && r.SubmissionRevision == submission.Revision)
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
        public IActionResult LookupStatus(StatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions
                    .Include(s => s.Audits)
                    .FirstOrDefault(s => s.Code.Equals(model.Code));

                if (submission == null)
                    return View("StatusNotFound");

                return RedirectToAction(nameof(ViewStatus), new { id = submission.Code });
            }

            //form not complete
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ViewStatus(string id)
        {
            var submission = _applicationDbContext.Submissions
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .FirstOrDefault(s => s.Code.Equals(id));

            submission.Audits = submission.Audits.OrderBy(a => a.DateTime).ToList();
            submission.Responses = submission.Responses.OrderBy(r => r.Created).ToList();

            if (submission == null)
                return View("StatusNotFound");

            return View(submission);
        }

        [AuthorizeRoles(RoleNames.Administrator)]
        public IActionResult Delete(int id)
        {
            var submission = _applicationDbContext.Submissions
                .Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            foreach (var r in submission.Reviews)
            {
                _applicationDbContext.Reviews.Remove(r);
            }
            foreach (var a in submission.Audits)
            {
                _applicationDbContext.Histories.Remove(a);
            }
            foreach (var r in submission.Responses)
            {
                _applicationDbContext.Responses.Remove(r);
            }
            foreach (var f in submission.Files)
            {
                _storage.DeleteFile(f.BlobName);
            }
            foreach (var c in submission.StateHistory)
            {
                _applicationDbContext.StateChanges.Remove(c);
            }
            _applicationDbContext.Submissions.Remove(submission);

            _applicationDbContext.SaveChanges();
            return RedirectToAction("List");
        }

        public async Task<ActionResult> File(int id)
        {
            var file = _applicationDbContext.Files
                    .FirstOrDefault(f => f.Id == id);

            if (file == null)
                return HttpNotFound("File not found");

            var stream = await _storage.RetriveFile(file.BlobName);
            return File(stream, "application/octet", file.Name);
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
            if (ModelState.IsValid && model.Files.Count > 0)
            {
                List<IFormFile> files = model.Files.ToList();

                //Validate files before continuing
                long totalSize = 0;
                foreach (var fileContent in files)
                {
                    var fileName = FormUtils.GetUploadedFilename(fileContent);
                    if(!FormUtils.IsValidFileType(fileName))
                    {
                        ModelState.AddModelError(string.Empty, "Invalid file type.");
                        return View(model);
                    }
                    totalSize += fileContent.Length;
                }

                if(totalSize > maxSizeBytes)
                {
                    ModelState.AddModelError(string.Empty, "Files too large.");
                    return View(model);
                }

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
                    Revision = 1,
                    StatusChangeTime = DateTime.Now,
                    SubmissionDate = DateTime.Now,
                    PrecedentSetting = false
                };

                foreach(var fileContent in files)
                {
                    var fileName = FormUtils.GetUploadedFilename(fileContent);
                    var blobId = await _storage.StoreFile(sub.Code, fileContent.OpenReadStream());
                    var file = new File
                    {
                        Name = fileName,
                        BlobName = blobId
                    };

                    sub.Files.Add(file);
                    _applicationDbContext.Files.Add(file);
                }

                AddStateSwitch(sub);

                AddHistoryEntry(sub, model.FirstName + " " + model.LastName, "Submitted");

                _applicationDbContext.Submissions.Add(sub);
                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, sub, _email);

                return View("PostCreate", sub);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        
        [HttpGet]
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.BoardChairman)]
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
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.BoardChairman)]
        public async Task<IActionResult> CheckCompleteness(ApproveRejectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.StateHistory).FirstOrDefault(s => s.Id == model.SubmissionId);
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
                {
                    submission.Status = Status.MissingInformation;
                    if(submission.Responses == null)
                        submission.Responses = new List<Response>();

                    var response = new Response
                    {
                        Created = DateTime.Now,
                        Comments = model.UserFeedback,
                        Submission = submission
                    };
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }

                AddStateSwitch(submission);
                submission.StatusChangeTime = DateTime.Now;

                var user = await _userManager.FindByIdAsync(User.GetUserId());                
                string action = string.Format("Marked {0}. Comments: {1}", model.Approve ? "complete" : "missing information", model.Comments);
                AddHistoryEntry(submission, user.FullName, action);

                submission.LastModified = DateTime.Now;
                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

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
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.Reviews).Include(s => s.StateHistory)
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
                    Submission = submission,
                    SubmissionRevision = submission.Revision
                };

                if (submission.Reviews == null)
                    submission.Reviews = new List<Review>();
                submission.Reviews.Add(review);

                AddHistoryEntry(submission, user.FullName, "Submitted review");

                _applicationDbContext.Reviews.Add(review);

                //Final review!
                if (submission.Reviews.Where(r => r.SubmissionRevision == submission.Revision).Count() == GetReviewerCount(_applicationDbContext))
                {
                    submission.Status = Status.ARBFinal;
                    submission.LastModified = DateTime.Now;
                    submission.StatusChangeTime = DateTime.Now;
                    AddHistoryEntry(submission, "System", "All reviews in, sent to chairman");
                    AddStateSwitch(submission);
                    EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);
                }
                submission.LastModified = DateTime.Now;

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

            TallyVotesViewModel model = new TallyVotesViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.BoardChairman)]
        public async Task<IActionResult> TallyVotes(TallyVotesViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.StateHistory).FirstOrDefault(s => s.Id == model.SubmissionId);
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

                    var response = new Response
                    {
                        Created = DateTime.Now,
                        Comments = model.UserFeedback,
                        Submission = submission
                    };
                    if (submission.Responses == null)
                        submission.Responses = new List<Response>();
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }
                else if (status == ReviewStatus.Rejected) //Still send rejections for final review
                {
                    submission.Status = Status.ReviewComplete;
                }
                else
                {
                    throw new Exception("Invalid option");
                }
                AddStateSwitch(submission);
                submission.StatusChangeTime = DateTime.Now;

                var user = await _userManager.FindByIdAsync(User.GetUserId());
                string action = string.Format("{0}. Comments: {1}", model.Status, model.Comments);
                AddHistoryEntry(submission, user.FullName, action);

                submission.LastModified = DateTime.Now;
                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.Include(s => s.Reviews).ThenInclude(r => r.Reviewer).FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.HOALiaison)]
        public IActionResult FinalCheck(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            FinalReview model = new FinalReview
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalCheck(FinalReview model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.StateHistory).FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return HttpNotFound("Submission not found");

                var user = await _userManager.FindByIdAsync(User.GetUserId());
                string action = string.Format("{0}. Comments: {1}", model.Status, model.Comments);
                AddHistoryEntry(submission, user.FullName, action);

                var status = (Status)Enum.Parse(typeof(Status), model.Status);
                if (status == Status.Approved)
                {
                    submission.Status = Status.PrepApproval;
                }
                else if (status == Status.ConditionallyApproved)
                {
                    submission.Status = Status.PrepConditionalApproval;

                    var response = new Response
                    {
                        Created = DateTime.Now,
                        Comments = model.UserFeedback,
                        Submission = submission
                    };
                    if (submission.Responses == null)
                        submission.Responses = new List<Response>();
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }
                else
                {
                    if (status == Status.MissingInformation)
                        submission.Status = Status.MissingInformation;
                    else
                        submission.Status = Status.Rejected;

                    var response = new Response
                    {
                        Created = DateTime.Now,
                        Comments = model.UserFeedback,
                        Submission = submission
                    };
                    if (submission.Responses == null)
                        submission.Responses = new List<Response>();
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }

                AddStateSwitch(submission);
                submission.StatusChangeTime = DateTime.Now;                
                submission.LastModified = DateTime.Now;
                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Resubmit(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id && (s.Status  == Status.Rejected || s.Status == Status.MissingInformation));
            if (submission == null)
                return HttpNotFound("Submission not found");

            ResubmitViewModel model = new ResubmitViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Resubmit(ResubmitViewModel model)
        {
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId && (s.Status == Status.Rejected || s.Status == Status.MissingInformation));
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Audits).Include(s => s.Reviews).Include(s => s.Files)
                    .FirstOrDefault(s => s.Id == model.SubmissionId && (s.Status == Status.Rejected || s.Status == Status.MissingInformation));
                if (submission == null)
                    return HttpNotFound("Submission not found");

                //Validate files before continuing
                List<IFormFile> files = model.Files == null ? new List<IFormFile>() : model.Files.ToList();
                long totalSize = 0;
                foreach (var fileContent in files)
                {
                    var fileName = FormUtils.GetUploadedFilename(fileContent);
                    if (!FormUtils.IsValidFileType(fileName))
                    {
                        ModelState.AddModelError(string.Empty, "Invalid file type.");
                        return View(model);
                    }
                    totalSize += fileContent.Length;
                }

                if (totalSize > maxSizeBytes)
                {
                    ModelState.AddModelError(string.Empty, "Files too large.");
                    return View(model);
                }

                submission.SubmissionDate = DateTime.Now;
                submission.StatusChangeTime = DateTime.Now;

                //Add new comments
                submission.Description = string.Format("{0}\n\nResubmitted {1}:\n\n{2}", submission.Description, DateTime.Now.ToString("MM/dd/yyyy hh:mm tt"), model.Description);

                //any new files
                if (model.Files != null)
                {
                    foreach (var fileContent in files)
                    {
                        var fileName = FormUtils.GetUploadedFilename(fileContent);
                        var blobId = await _storage.StoreFile(submission.Code, fileContent.OpenReadStream());
                        var file = new File
                        {
                            Name = fileName,
                            BlobName = blobId
                        };

                        submission.Files.Add(file);
                        _applicationDbContext.Files.Add(file);
                    }
                }

                //Increment revision
                submission.Revision = submission.Revision + 1;
                submission.Status = Status.Submitted;

                AddHistoryEntry(submission, submission.FirstName + " " + submission.LastName, "Resubmitted");

                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(ViewStatus), new { id = submission.Code });
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Retract(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.StateHistory).FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            if (submission.Status == Status.Approved ||
                submission.Status == Status.ConditionallyApproved ||
                submission.Status == Status.Rejected ||
                submission.Status == Status.Retracted ||
                submission.Status == Status.MissingInformation)
            {
                throw new Exception("Invliad state!");
            }

            AddStateSwitch(submission);
            submission.Status = Status.Retracted;
            AddHistoryEntry(submission, submission.FirstName + " " + submission.LastName, "Retracted");

            _applicationDbContext.SaveChanges();

            EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

            return RedirectToAction(nameof(ViewStatus), new { id = submission.Code });
        }

        [Authorize(Roles = RoleNames.BoardChairman)]
        public async Task<IActionResult> PrecedentSetting(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return HttpNotFound("Submission not found");

            var user = await _userManager.FindByIdAsync(User.GetUserId());

            if (submission.Status != Status.ARBIncoming &&
                submission.Status != Status.UnderReview)
            {
                throw new Exception("Invliad state!");
            }

            AddHistoryEntry(submission, user.FullName, "Marked as precedent setting");
            submission.PrecedentSetting = true;
            _applicationDbContext.SaveChanges();

            EmailHelper.NotifyPrecedentSetting(_applicationDbContext, submission, _email);

            return RedirectToAction(nameof(View), new { id = submission.Id });
        }
    }
}
