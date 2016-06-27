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
using HOA.Model.Backup.V1;
using Newtonsoft.Json;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace HOA.Controllers
{
    [RequireHttps]
    [Authorize]
    public class BackupController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IEmailSender _email;
        private readonly IFileStore _storage;
        private RoleManager<IdentityRole> _roleManager;

        public BackupController(ApplicationDbContext applicationDbContext, UserManager<ApplicationUser> userManager, IEmailSender emailSender, IFileStore store, RoleManager<IdentityRole> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _userManager = userManager;
            _email = emailSender;
            _roleManager = roleManager;
            _storage = store;
        }

        private List<SubmissionV1> GetBackupData()
        {
            List<SubmissionV1> results = new List<SubmissionV1>();

            IQueryable<Submission> subs = _applicationDbContext.Submissions;
            subs.Include(s => s.Audits)
                .Include(s => s.Comments).ThenInclude(c => c.User)
                .Include(s => s.Reviews).ThenInclude(r => r.Reviewer)
                .Include(s => s.Files)
                .Include(s => s.Responses)
                .Include(s => s.StateHistory)
                .OrderByDescending(s => s.SubmissionDate).ToList();

            foreach(var sub in subs)
            {
                var result = new SubmissionV1()
                {
                    Id = sub.Id,
                    Code = sub.Code,
                    FirstName = sub.FirstName,
                    LastName = sub.LastName,
                    Address = sub.Address,
                    Email = sub.Email,
                    Description = sub.Description,
                    Status = sub.Status.ToString(),
                    Revision = sub.Revision,
                    LastModified = sub.LastModified,
                    StatusChangeTime = sub.StatusChangeTime,
                    SubmissionDate = sub.SubmissionDate,
                    PrecedentSetting = sub.PrecedentSetting,
                    
                    FinalApprovalBlob = sub.FinalApprovalBlob,
                    FinalApprovalFileName = sub.FinalApprovalFileName,
                };

                //comments
                if(sub.Comments != null && sub.Comments.Count > 0)
                {
                    result.Comments = new List<CommentV1>();
                    foreach(var c in sub.Comments)
                    {
                        var comment = new CommentV1
                        {
                            UserEmail = c.User.Email,
                            Created = c.Created,
                            Comments = c.Comments
                        };
                        result.Comments.Add(comment);
                    }
                    result.Comments = result.Comments.OrderBy(c => c.Created).ToList();
                }

                //reviews
                if (sub.Reviews != null && sub.Reviews.Count > 0)
                {
                    result.Reviews = new List<ReviewV1>();
                    foreach (var r in sub.Reviews)
                    {
                        var review = new ReviewV1
                        {
                            ReviewerEmail = r.Reviewer.Email,
                            Status = r.Status.ToString(),
                            Created = r.Created,
                            Comments = r.Comments,
                            SubmissionRevision = r.SubmissionRevision
                        };
                        result.Reviews.Add(review);
                    }
                    result.Reviews = result.Reviews.OrderBy(r => r.Created).ToList();
                }

                //Audit/history
                if (sub.Audits != null && sub.Audits.Count > 0)
                {
                    result.Audits = new List<HistoryV1>();
                    foreach (var a in sub.Audits)
                    {
                        var audit = new HistoryV1
                        {
                            User = a.User,
                            DateTime = a.DateTime,
                            Action = a.Action,
                            Revision = a.Revision
                        };
                        result.Audits.Add(audit);
                    }
                    result.Audits = result.Audits.OrderBy(a => a.DateTime).ToList();
                }

                //Responses
                if (sub.Responses != null && sub.Responses.Count > 0)
                {
                    result.Responses = new List<ResponseV1>();
                    foreach (var r in sub.Responses)
                    {
                        var response = new ResponseV1
                        {
                            Created = r.Created,
                            Comments = r.Comments
                        };
                        result.Responses.Add(response);
                    }
                    result.Responses = result.Responses.OrderBy(r => r.Created).ToList();
                }

                //Files
                if (sub.Files != null && sub.Files.Count > 0)
                {
                    result.Files = new List<FileV1>();
                    foreach (var f in sub.Files)
                    {
                        var file = new FileV1
                        {
                            Name = f.Name,
                            BlobName = f.BlobName
                        };
                        result.Files.Add(file);
                    }
                }

                //State Changes
                if (sub.StateHistory != null && sub.StateHistory.Count > 0)
                {
                    result.StateHistory = new List<StateChangeV1>();
                    foreach (var h in sub.StateHistory)
                    {
                        var history = new StateChangeV1
                        {
                            StartTime = h.StartTime,
                            EndTime = h.EndTime,
                            State = h.State.ToString()
                        };
                        result.StateHistory.Add(history);
                    }
                    result.StateHistory = result.StateHistory.OrderBy(s => s.StartTime).ToList();
                }

                results.Add(result);
            }

            return results;
        }

        private List<UserV1> GetUserBackup()
        {
            List<UserV1> result = new List<UserV1>();
            var users = _applicationDbContext.Users.Include(u => u.Roles).ToList();
            var identityUsers = _userManager.Users.ToList();

            foreach (var user in users)
            {
                List<string> roles = new List<string>();

                foreach (var role in user.Roles)
                {
                    var roleName = _roleManager.FindByIdAsync(role.RoleId).Result.Name;

                    if (roleName.Equals(RoleNames.Administrator))
                        roleName = "Administrator";
                    else if (roleName.Equals(RoleNames.CommunityManager))
                        roleName = "Community Manager";
                    else if (roleName.Equals(RoleNames.BoardChairman))
                        roleName = "Board Chairman";
                    else if (roleName.Equals(RoleNames.ARBBoardMember))
                        roleName = "ARB Board Member";
                    else if (roleName.Equals(RoleNames.HOALiaison))
                        roleName = "HOA Liaison";

                    roles.Add(roleName);
                }

                var u = new UserV1
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Enabled = user.Enabled,
                    DisableNotification = user.DisableNotification,                    
                    Roles = roles,                    
                    Email = identityUsers.FirstOrDefault(iu => iu.Id == user.Id).Email
                };

                result.Add(u);
            }

            return result;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            DataSetV1 data = new DataSetV1();
            data.Submissions = GetBackupData();
            data.Users = GetUserBackup();

            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            return Json(data, settings);
        }
    }
}
