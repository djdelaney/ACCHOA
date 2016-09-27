﻿using System;
using System.Collections.Generic;
using System.Linq;
using HOA.Model;
using HOA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HOA.Model.Backup.V1;
using Newtonsoft.Json;
using System.Threading.Tasks;

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

            List<Submission> subs = _applicationDbContext.Submissions
                .Include(s => s.Audits)
                .Include(s => s.Comments).ThenInclude(c => c.User)
                .Include(s => s.Reviews).ThenInclude(r => r.Reviewer)
                .Include(s => s.Files)
                .Include(s => s.Responses)
                .Include(s => s.StateHistory)
                .OrderBy(s => s.SubmissionDate).ToList();

            foreach(var sub in subs)
            {
                sub.Files = sub.Files.OrderBy(s => s.Name).ThenBy(s=> s.BlobName).ToList();
                sub.Reviews = sub.Reviews.OrderBy(s => s.Created).ThenBy(s => s.Reviewer).ToList();
                sub.Audits = sub.Audits.OrderBy(s => s.DateTime).ThenBy(s => s.User).ToList();
                sub.Comments = sub.Comments.OrderBy(s => s.Created).ToList();
                sub.StateHistory = sub.StateHistory.OrderBy(s => s.StartTime).ToList();
                sub.Responses = sub.Responses.OrderBy(s => s.Created).ToList();

                var result = new SubmissionV1()
                {
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

            return result.OrderBy(u => u.Email).ToList();
        }

        // GET: /<controller>/
        public IActionResult Export()
        {
            DataSetV1 data = new DataSetV1();
            data.Submissions = GetBackupData();
            data.Users = GetUserBackup();

            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            return Json(data, settings);
        }

        public async Task<IActionResult> Import()
        {
            string json = System.IO.File.ReadAllText(@"C:\Users\Dan\Desktop\Backup 9-25-2016.json");
            DataSetV1 data = JsonConvert.DeserializeObject<DataSetV1>(json);

            //Import users
            foreach (var imported in data.Users)
            {
                var user = await _userManager.FindByEmailAsync(imported.Email);
                if (user == null)
                {
                    user = new ApplicationUser { UserName = imported.Email, Email = imported.Email, FirstName = imported.FirstName, LastName = imported.LastName, Enabled = imported.Enabled };
                    await _userManager.CreateAsync(user, "Password");//random GUID pwd. Reset later

                    foreach (var role in imported.Roles)
                    {
                        string trueName = null;
                        if (role.Equals("Board Chairman", StringComparison.OrdinalIgnoreCase))
                        {
                            trueName = RoleNames.BoardChairman;
                        }
                        else if (role.Equals("HOA Liaison", StringComparison.OrdinalIgnoreCase))
                        {
                            trueName = RoleNames.HOALiaison;
                        }
                        else if (role.Equals("Community Manager", StringComparison.OrdinalIgnoreCase))
                        {
                            trueName = RoleNames.CommunityManager;
                        }
                        else if (role.Equals("ARB Board Member", StringComparison.OrdinalIgnoreCase))
                        {
                            trueName = RoleNames.ARBBoardMember;
                        }
                        else if (role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                        {
                            trueName = RoleNames.Administrator;
                        }
                        else if (role.Equals("HOA Board Member", StringComparison.OrdinalIgnoreCase))
                        {
                            trueName = RoleNames.HOABoardMember;
                        }
                        else
                        {
                            throw new Exception("Invalid role name");
                        }

                        await _userManager.AddToRoleAsync(user, trueName);
                    }
                }
            }
            var identityUsers = _userManager.Users.ToList();

            //Now import submissions
            foreach (SubmissionV1 oldSub in data.Submissions)
            {
                ImportSubmission(oldSub, identityUsers);
            }
                        
            _applicationDbContext.SaveChanges();
            return Content("good");
        }

        private void ImportSubmission(SubmissionV1 oldSub, List<ApplicationUser> identityUsers)
        {
            Status stat = (HOA.Model.Status)Enum.Parse(typeof(HOA.Model.Status), oldSub.Status);

            Submission sub = new Submission
            {
                Code = oldSub.Code,
                FirstName = oldSub.FirstName,
                LastName = oldSub.LastName,
                Address = oldSub.Address,
                Email = oldSub.Email,
                Description = oldSub.Description,
                Status = stat,
                Revision = oldSub.Revision,
                LastModified = oldSub.LastModified,
                StatusChangeTime = oldSub.StatusChangeTime,
                SubmissionDate = oldSub.SubmissionDate,
                PrecedentSetting = oldSub.PrecedentSetting,
                FinalApprovalBlob = oldSub.FinalApprovalBlob,
                FinalApprovalFileName = oldSub.FinalApprovalFileName,
                Reviews = new List<Review>(),
                Audits = new List<History>(),
                Responses = new List<Response>(),
                Files = new List<File>(),
                StateHistory = new List<StateChange>(),
                Comments = new List<Comment>(),
            };

            if (oldSub.Reviews == null)
                oldSub.Reviews = new List<ReviewV1>();
            if (oldSub.Audits == null)
                oldSub.Audits = new List<HistoryV1>();
            if (oldSub.Responses == null)
                oldSub.Responses = new List<ResponseV1>();
            if (oldSub.Files == null)
                oldSub.Files = new List<FileV1>();
            if (oldSub.StateHistory == null)
                oldSub.StateHistory = new List<StateChangeV1>();
            if (oldSub.Comments == null)
                oldSub.Comments = new List<CommentV1>();

            foreach (var r in oldSub.Reviews)
            {
                Review review = new Review
                {
                    Reviewer = identityUsers.FirstOrDefault(u => u.Email.Equals(r.ReviewerEmail)),
                    Status = (HOA.Model.ReviewStatus)Enum.Parse(typeof(HOA.Model.ReviewStatus), r.Status),
                    Created = r.Created,
                    Comments = r.Comments,
                    Submission = sub,
                    SubmissionRevision = r.SubmissionRevision
                };
                sub.Reviews.Add(review);
                _applicationDbContext.Reviews.Add(review);
            }

            foreach (var a in oldSub.Audits)
            {
                History history = new History
                {
                    User = a.User,
                    DateTime = a.DateTime,
                    Action = a.Action,
                    Submission = sub,
                    Revision = a.Revision
                };
                sub.Audits.Add(history);
                _applicationDbContext.Histories.Add(history);
            }

            foreach (var r in oldSub.Responses)
            {
                Response response = new Response
                {
                    Created = r.Created,
                    Comments = r.Comments,
                    Submission = sub
                };
                sub.Responses.Add(response);
                _applicationDbContext.Responses.Add(response);
            }

            foreach (var f in oldSub.Files)
            {
                File file = new File
                {
                    Name = f.Name,
                    BlobName = f.BlobName,
                    Submission = sub
                };
                sub.Files.Add(file);
                _applicationDbContext.Files.Add(file);
            }

            foreach (var s in oldSub.StateHistory)
            {
                StateChange history = new StateChange
                {
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    State = (HOA.Model.Status)Enum.Parse(typeof(HOA.Model.Status), s.State),
                    Submission = sub
                };
                sub.StateHistory.Add(history);
                _applicationDbContext.StateChanges.Add(history);
            }

            foreach (var c in oldSub.Comments)
            {
                Comment comment = new Comment
                {
                    User = identityUsers.FirstOrDefault(u => u.Email.Equals(c.UserEmail)),
                    Created = c.Created,
                    Comments = c.Comments,
                    Submission = sub
                };
                sub.Comments.Add(comment);
                _applicationDbContext.Comments.Add(comment);
            }

            _applicationDbContext.Submissions.Add(sub);
        }
    }
}
