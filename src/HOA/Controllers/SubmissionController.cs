using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HOA.Model;
using HOA.Model.ViewModel;
using HOA.Util;
using HOA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HOA.Controllers
{
    [RequireHttps]
    [Authorize]
    public class SubmissionController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;
        private readonly IFileStore _storage;
        private RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<SubmissionController> _logger;

        private const int maxSizeBytes = 15 * 1024 * 1024; //10MB

        public SubmissionController(ApplicationDbContext applicationDbContext,
                                    UserManager<ApplicationUser> userManager,
                                    IEmailSender emailSender,
                                    IFileStore store,
                                    RoleManager<IdentityRole> roleManager,
                                    ILogger<SubmissionController> logger)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _email = emailSender;
            _roleManager = roleManager;
            _storage = store;
            _logger = logger;
        }

        public static int GetReviewerCount(ApplicationDbContext context, bool includeLandscaping)
        {
            var role = context.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(RoleNames.ARBBoardMember));
            List<string> userIds = role.Users.Select(u => u.UserId).ToList();
            var users = context.Users.Where(u => userIds.Contains(u.Id) && u.Enabled &&
                (!u.LandscapingMember || includeLandscaping)
            );
            return users.Count();
        }

        public void AddStateSwitch(Submission s)
        {
            if (s.StateHistory == null)
                s.StateHistory = new List<StateChange>();

            StateChange existing = s.StateHistory.FirstOrDefault(h => h.EndTime.Equals(DateTime.MinValue));
            if(existing != null)
            {
                existing.EndTime = DateTime.UtcNow;
            }

            //Dont log duration for final statuses
            if(s.Status == Status.Approved || s.Status == Status.ConditionallyApproved)
            {
                return;
            }

            var change = new StateChange
            {
                StartTime = DateTime.UtcNow,
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
                DateTime = DateTime.UtcNow,
                Action = action,
                Submission = s
            };
            s.Audits.Add(history);
            _applicationDbContext.Histories.Add(history);
        }

        public void AddInternalComment(Submission s, ApplicationUser user, string text)
        {
            if (s.Comments == null)
                s.Comments = new List<Comment>();

            var comment = new Comment
            {
                User = user,
                Created = DateTime.UtcNow,
                Comments = text,
                Submission = s
            };
            s.Comments.Add(comment);
            _applicationDbContext.Comments.Add(comment);
        }

        [HttpGet]
        [Authorize(Roles = "CommunityManager")]
        public IActionResult FinalResponse(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

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
                IFormFile file = null;
                if (model.Files != null && model.Files.Count > 0)
                {
                    List<IFormFile> files = model.Files.ToList();
                    file = files.FirstOrDefault();
                }
                bool hasAttachment = (file != null);

                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                if (submission.Status != Status.FinalResponse)
                    throw new Exception("Invalid status");

                if(submission.ReturnStatus != ReturnStatus.Approved && submission.ReturnStatus != ReturnStatus.ConditionallyApproved)
                    throw new Exception("Invalid return status");

                var user = await _userManager.GetUserAsync(HttpContext.User);

                if (submission.ReturnStatus == ReturnStatus.Approved)
                    submission.Status = Status.Approved;
                else if (submission.ReturnStatus == ReturnStatus.ConditionallyApproved)
                    submission.Status = Status.ConditionallyApproved;
                else
                    throw new Exception("Invalid status");

                AddStateSwitch(submission);

                //Store approval file
                if (hasAttachment)
                {
                    submission.ResponseDocumentFileName = FormUtils.GetUploadedFilename(file);
                    submission.ResponseDocumentBlob = await _storage.StoreFile(submission.Code, file.OpenReadStream());
                }

                //Any final comments?
                if (!string.IsNullOrEmpty(model.UserFeedback))
                {
                    var response = new Response
                    {
                        Created = DateTime.UtcNow,
                        Comments = model.UserFeedback,
                        Submission = submission
                    };
                    if (submission.Responses == null)
                        submission.Responses = new List<Response>();
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }

                AddHistoryEntry(submission, user.FullName, "Sent final response");

                if(!string.IsNullOrEmpty(model.Comments))
                    AddInternalComment(submission, user, model.Comments);

                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();

                System.IO.Stream attachment = null;
                if (hasAttachment)
                {
                    attachment = await _storage.RetriveFile(submission.ResponseDocumentBlob);
                }
                EmailHelper.NotifyFinalResponse(_applicationDbContext, submission, model.UserFeedback, _email, attachment, submission.ResponseDocumentFileName);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "CommunityManager")]
        public IActionResult CommunityMgrReturn(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            ReturnCommentsViewModel model = new ReturnCommentsViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.CommunityManager)]
        public async Task<IActionResult> CommunityMgrReturn(ReturnCommentsViewModel model)
        {
            if (ModelState.IsValid)
            {
                IFormFile file = null;
                if (model.Files != null && model.Files.Count == 1)
                {
                    List<IFormFile> files = model.Files.ToList();
                    file = files.FirstOrDefault();
                }
                bool hasAttachment = (file != null);

                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                if (submission.Status != Status.CommunityMgrReturn)
                    throw new Exception("Invalid status");
                
                if (submission.ReturnStatus != ReturnStatus.MissingInformation && submission.ReturnStatus != ReturnStatus.Reject)
                    throw new Exception("Invalid return status");

                //attachments required for rejections
                if (!hasAttachment && submission.ReturnStatus == ReturnStatus.Reject)
                {
                    model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
                    model.SubmissionId = model.Submission.Id;
                    ModelState.AddModelError(string.Empty, "File required.");
                    return View(model);
                }

                if (hasAttachment)
                {
                    submission.ResponseDocumentFileName = FormUtils.GetUploadedFilename(file);
                    submission.ResponseDocumentBlob = await _storage.StoreFile(submission.Code, file.OpenReadStream());
                }

                var user = await _userManager.GetUserAsync(HttpContext.User);

                if (submission.ReturnStatus == ReturnStatus.MissingInformation)
                    submission.Status = Status.MissingInformation;
                else if (submission.ReturnStatus == ReturnStatus.Reject)
                    submission.Status = Status.Rejected;
                else
                    throw new Exception("Invalid status");
                
                AddStateSwitch(submission);
                
                var response = new Response
                {
                    Created = DateTime.UtcNow,
                    Comments = model.UserFeedback,
                    Submission = submission
                };
                if (submission.Responses == null)
                    submission.Responses = new List<Response>();
                submission.Responses.Add(response);
                _applicationDbContext.Responses.Add(response);
                
                AddHistoryEntry(submission, user.FullName, "Sent response");
                
                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();

                System.IO.Stream attachment = null;
                if (submission.Status == Status.Rejected)
                {
                    attachment = await _storage.RetriveFile(submission.ResponseDocumentBlob);
                }
                EmailHelper.NotifyFinalResponse(_applicationDbContext, submission, model.UserFeedback, _email, attachment, submission.ResponseDocumentFileName);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }


        public IActionResult List(int page = 1, string filter = null)
        {
            IQueryable<Submission> subs = _applicationDbContext.Submissions;

            if(string.IsNullOrEmpty(filter))
            {
                filter = "Todo";
            }

            if (filter.Equals("Todo"))
            {
                if (User.IsInRole(RoleNames.CommunityManager))
                {
                    subs = subs.Where(s => s.Status != Status.Rejected &&
                                  s.Status != Status.MissingInformation &&
                                  s.Status != Status.Retracted);
                }
                else
                {
                    subs = subs.Where(s => s.Status != Status.Approved &&
                                  s.Status != Status.Rejected &&
                                  s.Status != Status.ConditionallyApproved &&
                                  s.Status != Status.MissingInformation &&
                                  s.Status != Status.Retracted);
                }

                if (User.IsInRole(RoleNames.Administrator))
                {
                }
                else if (User.IsInRole(RoleNames.CommunityManager))
                {
                    subs = subs.Where(s => s.Status == Status.CommunityMgrReview ||
                                        s.Status == Status.FinalResponse || 
                                        s.Status == Status.CommunityMgrReturn ||
                                        (s.ResponseDocumentBlob == null && (s.Status == Status.Approved || s.Status == Status.ConditionallyApproved)));
                                        //include approved items missing final attachment
                }
                else if (User.IsInRole(RoleNames.BoardChairman))
                {
                    subs = subs.Where(s => s.Status == Status.ARBChairReview || s.Status == Status.ARBTallyVotes);
                }
                else if (User.IsInRole(RoleNames.ARBBoardMember))
                {
                    subs = subs.Where(s => s.Status == Status.CommitteeReview);

                    //Filter to ones that the current user hasnt reviewed
                    var user = _userManager.GetUserAsync(HttpContext.User).Result;
                    subs = subs.Where(s => !s.Reviews.Any(r => r.Reviewer.Id == user.Id && r.SubmissionRevision == s.Revision));

                    //Filter non landscaping requests
                    if (user.LandscapingMember)
                    {
                        subs = subs.Where(s => s.LandscapingRelated);
                    }
                }
                else if (User.IsInRole(RoleNames.HOALiaison))
                {
                    subs = subs.Where(s => s.Status == Status.HOALiasonReview || s.Status == Status.HOALiasonInput);
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
                DateTime oneMonthAgo = DateTime.UtcNow.AddDays(-30);
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

            var pager = new Pager(subs.Count(), page);
            IList<Submission> results = subs.Include(s => s.Audits).OrderByDescending(s => s.LastModified).Skip((pager.CurrentPage - 1) * pager.PageSize).Take(pager.PageSize).ToList();

            var viewModel = new ViewSubmissionsViewModel()
            {
                Filter = filter,
                Submissions = results,
                Pager = pager
            };

            return View(viewModel);
        }

        public ActionResult View(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .ThenInclude(r => r.Reviewer)
                .Include(s => s.Audits)
                .Include(s => s.Files)
                .Include(s => s.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefault(s => s.Id == id);
            if(submission == null)
                return NotFound("Submission not found");

            submission.Comments = submission.Comments.OrderByDescending(c => c.Created).ToList();
            submission.Audits = submission.Audits.OrderByDescending(a => a.DateTime).ToList();

            var user = _userManager.GetUserAsync(HttpContext.User).Result;

            var model = new ViewSubmissionViewModel()
            {
                Submission = submission,
                ReviewerCount = GetReviewerCount(_applicationDbContext, submission.LandscapingRelated),
                CurrentReviewCount = submission.Reviews.Count(r => r.SubmissionRevision == submission.Revision),
                Reviewed = submission.Reviews.Any(r => r.Reviewer.Id == _userManager.GetUserId(User) && r.SubmissionRevision == submission.Revision),
                HideReviewOption = !submission.LandscapingRelated && user.LandscapingMember
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
        
        [AuthorizeRoles(RoleNames.BoardChairman)]
        public async Task<IActionResult> SkipQuorum(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            if (submission.Status != Status.CommitteeReview)
            {
                throw new Exception("Invliad state!");
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);

            submission.Status = Status.ARBTallyVotes;
            submission.LastModified = DateTime.UtcNow;
            submission.StatusChangeTime = DateTime.UtcNow;
            AddHistoryEntry(submission, user.FullName, "Manually skiped quorum");
            AddStateSwitch(submission);
            _applicationDbContext.SaveChanges();

            EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

            return RedirectToAction(nameof(View), new { id = submission.Id });
        }
        
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.Administrator)]
        public IActionResult Delete(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

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

            if(!string.IsNullOrEmpty(submission.ResponseDocumentBlob))
                _storage.DeleteFile(submission.ResponseDocumentBlob);

            _applicationDbContext.Submissions.Remove(submission);

            _applicationDbContext.SaveChanges();
            return RedirectToAction("List");
        }

        public async Task<ActionResult> File(int id)
        {
            var file = _applicationDbContext.Files
                    .FirstOrDefault(f => f.Id == id);

            if (file == null)
                return NotFound("File not found");

            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = file.Name,
                Inline = true  // false = prompt the user for downloading;  true = browser to try to show the file inline
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");

            //Supported types: ".pdf", ".jpg", ".jpeg", ".gif", ".png"
            string extension = System.IO.Path.GetExtension(file.Name);
            string contentType = "application/octet";
            if (extension.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                contentType = "application/pdf";
            if (extension.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                contentType = "image/png";
            if (extension.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || extension.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                contentType = "image/jpeg";
            if (extension.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                contentType = "image/gif";
            
            var stream = await _storage.RetriveFile(file.BlobName);
            return File(stream, contentType);
        }

        public async Task<ActionResult> ResponseFile(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);

            if (submission == null)
                return NotFound("File not found");

            var stream = await _storage.RetriveFile(submission.ResponseDocumentBlob);
            return File(stream, "application/octet", submission.ResponseDocumentFileName);
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
            try
            {
                if (ModelState.IsValid && model.Files != null && model.Files.Count > 0)
                {
                    List<IFormFile> files = model.Files.ToList();

                    if (files.Count == 0)
                    {
                        ModelState.AddModelError(string.Empty, "File required.");
                        return View(model);
                    }

                    //Validate files before continuing
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

                    var sub = new Submission()
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Address = model.Address,
                        Email = model.Email,
                        Description = model.Description,
                        Status = Status.CommunityMgrReview,
                        ReturnStatus = ReturnStatus.None,
                        LastModified = DateTime.UtcNow,
                        Code = DBUtil.GenerateUniqueCode(_applicationDbContext),
                        Revision = 1,
                        StatusChangeTime = DateTime.UtcNow,
                        SubmissionDate = DateTime.UtcNow,
                        PrecedentSetting = false,
                        Reviews = new List<Review>(),
                        Audits = new List<History>(),
                        Responses = new List<Response>(),
                        Files = new List<File>(),
                        StateHistory = new List<StateChange>(),
                        Comments = new List<Comment>(),
                    };

                    foreach (var fileContent in files)
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
                else if(model.Files == null || model.Files.Count == 0)
                {
                    ModelState.AddModelError(string.Empty, "Files are required");
                }

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch(Exception e)
            {
                _logger.LogError("Error creating submission", e);
                throw;
            }
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.BoardChairman, RoleNames.Administrator)]
        public IActionResult CheckCompleteness(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            if (!User.IsInRole(RoleNames.Administrator))
            {
                if (submission.Status == Status.CommunityMgrReview && !User.IsInRole(RoleNames.CommunityManager))
                    return NotFound("Not authorized");
                if (submission.Status == Status.ARBChairReview && !User.IsInRole(RoleNames.BoardChairman))
                    return NotFound("Not authorized");
            }

            CheckCompletenessViewModel model = new CheckCompletenessViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id,
                LandscapingRelated = submission.LandscapingRelated
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.BoardChairman, RoleNames.Administrator)]
        public async Task<IActionResult> CheckCompleteness(CheckCompletenessViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                submission.LandscapingRelated = model.LandscapingRelated;

                if (model.Approve)
                {
                    if (submission.Status == Status.CommunityMgrReview)
                    {
                        submission.Status = Status.ARBChairReview;
                    }
                    else
                    {
                        submission.Status = Status.CommitteeReview;
                    }
                }
                else
                {
                    //Send to mgr for formal response
                    submission.ReturnStatus = ReturnStatus.MissingInformation;
                    submission.Status = Status.CommunityMgrReturn;
                }

                AddStateSwitch(submission);
                submission.StatusChangeTime = DateTime.UtcNow;

                var user = await _userManager.GetUserAsync(HttpContext.User);
                string action = string.Format("Marked {0}", model.Approve ? "complete" : "missing information");
                AddHistoryEntry(submission, user.FullName, action);

                if (!string.IsNullOrEmpty(model.Comments))
                    AddInternalComment(submission, user, model.Comments);

                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }

        
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                    .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (submission.Reviews != null && submission.Reviews.Any(r => r.Reviewer.Id == user.Id && r.SubmissionRevision == submission.Revision) ||
                (!submission.LandscapingRelated && user.LandscapingMember))
            {
                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            ReviewSubmissionViewModel model = new ReviewSubmissionViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.ARBBoardMember)]
        public async Task<IActionResult> Review(ReviewSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (((ReviewStatus) Enum.Parse(typeof(ReviewStatus), model.Status)) != ReviewStatus.Approved &&
                    string.IsNullOrEmpty(model.Comments))
                {
                    ModelState.AddModelError(string.Empty, "You must supply comments for non-approvals.");
                    model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
                    return View(model);
                }

                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .Include(s => s.Reviews)
                    .ThenInclude(r => r.Reviewer)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                var user = await _userManager.GetUserAsync(HttpContext.User);
                if (submission.Reviews != null && submission.Reviews.Any(r => r.Reviewer.Id == user.Id && r.SubmissionRevision == submission.Revision))
                {
                    throw new Exception("Already reviewed!");
                }

                //Check landscaping
                if (!submission.LandscapingRelated && user.LandscapingMember)
                {
                    throw new Exception("NOT landscaping");
                }

                var review = new Review
                {
                    Reviewer = user,
                    Status = (ReviewStatus)Enum.Parse(typeof(ReviewStatus), model.Status),
                    Created = DateTime.UtcNow,
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
                if (submission.Reviews.Where(r => r.SubmissionRevision == submission.Revision).Count() == GetReviewerCount(_applicationDbContext, submission.LandscapingRelated))
                {
                    submission.Status = Status.ARBTallyVotes;
                    submission.LastModified = DateTime.UtcNow;
                    submission.StatusChangeTime = DateTime.UtcNow;
                    AddHistoryEntry(submission, "System", "All reviews in, sent to chairman");
                    AddStateSwitch(submission);
                    EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);
                }
                submission.LastModified = DateTime.UtcNow;

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
                return NotFound("Submission not found");

            TallyVotesViewModel model = new TallyVotesViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.BoardChairman, RoleNames.Administrator)]
        public async Task<IActionResult> TallyVotes(TallyVotesViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                var status = (TallyStatus)Enum.Parse(typeof(TallyStatus), model.Status);

                if (status == TallyStatus.Approved || status == TallyStatus.ConditionallyApproved || status == TallyStatus.Rejected)
                {
                    submission.Status = Status.HOALiasonReview;
                }
                else if (status == TallyStatus.MissingInformation)
                {
                    submission.ReturnStatus = ReturnStatus.MissingInformation;
                    submission.Status = Status.CommunityMgrReturn;
                }
                else if (status == TallyStatus.HOAInputRequired)
                {
                    submission.Status = Status.HOALiasonInput;
                }
                else
                {
                    throw new Exception("Invalid option");
                }

                AddStateSwitch(submission);
                submission.StatusChangeTime = DateTime.UtcNow;

                var user = await _userManager.GetUserAsync(HttpContext.User);
                string action = string.Format("Tally votes: {0}", model.Status);
                AddHistoryEntry(submission, user.FullName, action);

                if (!string.IsNullOrEmpty(model.Comments))
                    AddInternalComment(submission, user, model.Comments);

                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();
                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.Include(s => s.Reviews).ThenInclude(r => r.Reviewer).FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.HOALiaison, RoleNames.Administrator)]
        public IActionResult FinalCheck(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            FinalReview model = new FinalReview
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.HOALiaison, RoleNames.Administrator)]
        public async Task<IActionResult> FinalCheck(FinalReview model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                var user = await _userManager.GetUserAsync(HttpContext.User);
                if (!string.IsNullOrEmpty(model.Comments))
                    AddInternalComment(submission, user, model.Comments);
                string action = string.Format("Final Check {0}", model.Status);
                AddHistoryEntry(submission, user.FullName, action);

                //Store status for return
                submission.ReturnStatus = (ReturnStatus)Enum.Parse(typeof(ReturnStatus), model.Status);

                if (submission.ReturnStatus == ReturnStatus.Approved ||
                    submission.ReturnStatus == ReturnStatus.ConditionallyApproved)
                {
                    submission.Status = Status.FinalResponse;
                }
                else if (submission.ReturnStatus == ReturnStatus.MissingInformation ||
                    submission.ReturnStatus == ReturnStatus.Reject)
                {
                    submission.Status = Status.CommunityMgrReturn;
                }
                else
                {
                    throw new Exception("Invalid status");
                }

                AddStateSwitch(submission);
                submission.StatusChangeTime = DateTime.UtcNow;
                submission.LastModified = DateTime.UtcNow;
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
                return NotFound("Submission not found");

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
            model.Submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == model.SubmissionId && (s.Status == Status.Rejected || s.Status == Status.MissingInformation));
            if (ModelState.IsValid)
            {
                var submission = model.Submission;
                if (submission == null)
                    return NotFound("Submission not found");

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

                submission.SubmissionDate = DateTime.UtcNow;
                submission.StatusChangeTime = DateTime.UtcNow;

                //Add new comments
                submission.Description = string.Format("Resubmitted {0}:\n\n{1}\n\n{2}", model.Description, DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm tt"), submission.Description);
                if (submission.Description.Length > 2047)
                    submission.Description = submission.Description.Substring(0, 2047); //todo, expand this field

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
                submission.Status = Status.CommunityMgrReview;

                AddHistoryEntry(submission, submission.FirstName + " " + submission.LastName, "Resubmitted");

                submission.LastModified = DateTime.UtcNow;
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
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

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

            submission.LastModified = DateTime.UtcNow;
            _applicationDbContext.SaveChanges();

            EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

            return RedirectToAction(nameof(ViewStatus), new { id = submission.Code });
        }
        
        [Authorize(Roles = RoleNames.BoardChairman)]
        public async Task<IActionResult> GetHOAInput(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (submission.Status != Status.ARBTallyVotes)
            {
                throw new Exception("Invliad state!");
            }

            submission.Status = Status.HOALiasonInput;
            AddStateSwitch(submission);

            AddHistoryEntry(submission, user.FullName, "Sent to HOA for input");
            submission.LastModified = DateTime.UtcNow;
            _applicationDbContext.SaveChanges();

            EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

            return RedirectToAction(nameof(View), new { id = submission.Id });
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.HOALiaison)]
        public IActionResult LiasonInput(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            CommentsViewModel model = new CommentsViewModel
            {
                SubmissionId = submission.Id,
                Submission = submission
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.HOALiaison)]
        public async Task<IActionResult> LiasonInput(CommentsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                if (submission.Status != Status.HOALiasonInput)
                {
                    throw new Exception("Invliad state!");
                }

                var user = await _userManager.GetUserAsync(HttpContext.User);

                AddInternalComment(submission, user, model.Comments);

                AddHistoryEntry(submission, user.FullName, "Added HOA Input");
                submission.LastModified = DateTime.UtcNow;

                submission.Status = Status.ARBTallyVotes;
                AddStateSwitch(submission);

                _applicationDbContext.SaveChanges();

                EmailHelper.NotifyStatusChanged(_applicationDbContext, submission, _email);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }

        [Authorize(Roles = RoleNames.BoardChairman)]
        public async Task<IActionResult> PrecedentSetting(int id)
        {
            var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (submission.Status != Status.ARBChairReview &&
                submission.Status != Status.CommitteeReview)
            {
                throw new Exception("Invliad state!");
            }

            AddHistoryEntry(submission, user.FullName, "Marked as precedent setting");
            submission.PrecedentSetting = true;
            submission.LastModified = DateTime.UtcNow;
            _applicationDbContext.SaveChanges();

            EmailHelper.NotifyPrecedentSetting(_applicationDbContext, submission, _email);

            return RedirectToAction(nameof(View), new { id = submission.Id });
        }
        
        [HttpGet]
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.Administrator)]
        public IActionResult Edit(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            EditSubmissionViewModel model = new EditSubmissionViewModel
            {
                SubmissionId = submission.Id,
                FirstName = submission.FirstName,
                LastName = submission.LastName,
                Address = submission.Address,
                Email = submission.Email,
                Description = submission.Description,
                LandscapingRelated = submission.LandscapingRelated
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.CommunityManager, RoleNames.Administrator)]
        public async Task<IActionResult> Edit(EditSubmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                List<IFormFile> files = new List<IFormFile>();
                if(model.Files != null && model.Files.Count > 0)
                {
                    foreach(var fileContent in model.Files)
                    {
                        var fileName = FormUtils.GetUploadedFilename(fileContent);
                        if (!FormUtils.IsValidFileType(fileName))
                        {
                            ModelState.AddModelError(string.Empty, "Invalid file type.");
                            return View(model);
                        }
                        
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


                var user = await _userManager.GetUserAsync(HttpContext.User);

                submission.Address = model.Address;
                submission.FirstName = model.FirstName;
                submission.LastName = model.LastName;
                submission.Email = model.Email;
                submission.Description = model.Description;
                submission.LandscapingRelated = model.LandscapingRelated;

                AddHistoryEntry(submission, user.FullName, "Edited submission details");
                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Search()
        {
            return View(new SearchViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Search(SearchViewModel model)
        {
            if (ModelState.IsValid)
            {
                IQueryable<Submission> subs = _applicationDbContext.Submissions;

                if (!string.IsNullOrEmpty(model.Code))
                {
                    subs = subs.Where(s => s.Code.Contains(model.Code));
                    model.Code = model.Code.ToUpper();
                }

                if (!string.IsNullOrEmpty(model.Address))
                    subs = subs.Where(s => s.Address.IndexOf(model.Address, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(model.Name))
                    subs = subs.Where(s => string.Format("{0} {1}", s.FirstName, s.LastName).IndexOf(model.Name, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!string.IsNullOrEmpty(model.Description))
                    subs = subs.Where(s => s.Description.IndexOf(model.Description, StringComparison.OrdinalIgnoreCase) >= 0);

                var resultModel = new SearchResultsViewModel()
                {
                    Submissions = subs.Take(20).Include(s => s.Audits).OrderBy(s => s.LastModified).ToList()
                };

                return View("SearchResults", resultModel);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        
        [HttpGet]
        [Authorize(Roles = "CommunityManager")]
        public IActionResult AddApprovalDoc(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            AttachmentViewModel model = new AttachmentViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.CommunityManager)]
        public async Task<IActionResult> AddApprovalDoc(AttachmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                IFormFile file = null;
                if (model.Files != null && model.Files.Count > 0)
                {
                    List<IFormFile> files = model.Files.ToList();
                    file = files.FirstOrDefault();
                }
                bool hasAttachment = (file != null);

                var submission = _applicationDbContext.Submissions.Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                if (submission.Status != Status.Approved && submission.Status != Status.ConditionallyApproved)
                    throw new Exception("Incorrect status");

                if (!hasAttachment)
                {
                    model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
                    model.SubmissionId = model.Submission.Id;
                    ModelState.AddModelError(string.Empty, "File required.");
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(HttpContext.User);

                submission.ResponseDocumentFileName = FormUtils.GetUploadedFilename(file);
                submission.ResponseDocumentBlob = await _storage.StoreFile(submission.Code, file.OpenReadStream());

                AddHistoryEntry(submission, user.FullName, "Added response document");
                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();

                System.IO.Stream attachment = await _storage.RetriveFile(submission.ResponseDocumentBlob);
                EmailHelper.NotifyFinalResponse(_applicationDbContext, submission, model.UserFeedback, _email, attachment, submission.ResponseDocumentFileName);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            model.SubmissionId = model.Submission.Id;
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "CommunityManager")]
        public IActionResult QuickApprove(int id)
        {
            var submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == id);
            if (submission == null)
                return NotFound("Submission not found");

            QuickApproveViewModel model = new QuickApproveViewModel
            {
                Submission = submission,
                SubmissionId = submission.Id
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.CommunityManager)]
        public async Task<IActionResult> QuickApprove(QuickApproveViewModel model)
        {
            if (ModelState.IsValid)
            {
                IFormFile file = null;
                if (model.Files != null && model.Files.Count > 0)
                {
                    List<IFormFile> files = model.Files.ToList();
                    file = files.FirstOrDefault();
                }
                bool hasAttachment = (file != null);

                var submission = _applicationDbContext.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == model.SubmissionId);
                if (submission == null)
                    return NotFound("Submission not found");

                if (submission.Status != Status.CommunityMgrReview)
                    return NotFound("Incorrect status");

                var user = await _userManager.GetUserAsync(HttpContext.User);

                submission.Status = Status.Approved;
                AddStateSwitch(submission);

                //Store approval file
                if (hasAttachment)
                {
                    submission.ResponseDocumentFileName = FormUtils.GetUploadedFilename(file);
                    submission.ResponseDocumentBlob = await _storage.StoreFile(submission.Code, file.OpenReadStream());
                }

                //Any final comments?
                if (!string.IsNullOrEmpty(model.UserFeedback))
                {
                    var response = new Response
                    {
                        Created = DateTime.UtcNow,
                        Comments = model.UserFeedback,
                        Submission = submission
                    };
                    if (submission.Responses == null)
                        submission.Responses = new List<Response>();
                    submission.Responses.Add(response);
                    _applicationDbContext.Responses.Add(response);
                }

                AddHistoryEntry(submission, user.FullName, "Sent final response");

                if (!string.IsNullOrEmpty(model.Comments))
                    AddInternalComment(submission, user, model.Comments);

                submission.LastModified = DateTime.UtcNow;
                _applicationDbContext.SaveChanges();

                System.IO.Stream attachment = null;
                if (hasAttachment)
                {
                    attachment = await _storage.RetriveFile(submission.ResponseDocumentBlob);
                }
                EmailHelper.NotifyFinalResponse(_applicationDbContext, submission, model.UserFeedback, _email, attachment, submission.ResponseDocumentFileName);

                return RedirectToAction(nameof(View), new { id = submission.Id });
            }

            // If we got this far, something failed, redisplay form
            model.Submission = _applicationDbContext.Submissions.FirstOrDefault(s => s.Id == model.SubmissionId);
            return View(model);
        }
    }
}
