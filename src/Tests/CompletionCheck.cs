using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HOA.Controllers;
using HOA.Model;
using HOA.Model.ViewModel;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using HOA.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Helpers;

namespace Tests
{
    public class CompletionCheck : TestBase
    {
        [Fact]
        public void CommunityReview_Approve()
        {
            Setup("josh.rozzi@fsresidential.com");
            Assert.Equal(Status.CommunityMgrReview, _sub.Status);
            
            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = true,
                Comments = "COMMENT",
                Submission = null
            };

            RedirectToActionResult result = _controller.CheckCompleteness(vm).Result as RedirectToActionResult;

            //should redirect to submission id
            Assert.NotNull(result);
            Assert.Equal(_sub.Id, result.RouteValues.Values.FirstOrDefault());

            _sub = _db.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should have moved to ARB chair
            Assert.Equal(Status.ARBChairReview, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.ARBChairReview, change.State);

            //email to ARB chair
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("kfinnis@gmail.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void ARBReview_Approve()
        {
            Setup("kfinnis@gmail.com");

            _sub.Status = Status.ARBChairReview;
            _db.SaveChanges();

            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = true,
                Comments = "COMMENT",
                Submission = null
            };

            RedirectToActionResult result = _controller.CheckCompleteness(vm).Result as RedirectToActionResult;

            //should redirect to submission id
            Assert.NotNull(result);
            Assert.Equal(_sub.Id, result.RouteValues.Values.FirstOrDefault());

            _sub = _db.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should have moved to review
            Assert.Equal(Status.CommitteeReview, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.CommitteeReview, change.State);

            //email reviewers
            Assert.Equal(3, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("dletscher@brenntag.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void CommunityReview_MissingInformation()
        {
            Setup("josh.rozzi@fsresidential.com");
            Assert.Equal(Status.CommunityMgrReview, _sub.Status);

            _sub.Status = Status.CommunityMgrReview;
            _db.SaveChanges();

            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = false,
                Comments = "MissingInfo",
                Submission = null
            };

            RedirectToActionResult result = _controller.CheckCompleteness(vm).Result as RedirectToActionResult;

            //should redirect to submission id
            Assert.NotNull(result);
            Assert.Equal(_sub.Id, result.RouteValues.Values.FirstOrDefault());

            _sub = _db.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should have moved to community manager response
            Assert.Equal(Status.CommunityMgrReturn, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("MissingInfo", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.CommunityMgrReturn, change.State);

            //email community mgr
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void ARBReview_MissingInformation()
        {
            Setup("kfinnis@gmail.com");

            _sub.Status = Status.ARBChairReview;
            _db.SaveChanges();

            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = false,
                Comments = "MissingInfo",
                Submission = null
            };

            RedirectToActionResult result = _controller.CheckCompleteness(vm).Result as RedirectToActionResult;

            //should redirect to submission id
            Assert.NotNull(result);
            Assert.Equal(_sub.Id, result.RouteValues.Values.FirstOrDefault());

            _sub = _db.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should have moved to community manager return
            Assert.Equal(Status.CommunityMgrReturn, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("MissingInfo", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.CommunityMgrReturn, change.State);

            //email community mgr
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void EditSubmission()
        {
            Setup("kfinnis@gmail.com");
            
            EditSubmissionViewModel vm = new EditSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Address = "A",
                Description = "D",
                Email = "E",
                Files = new List<IFormFile>(),
                FirstName = "F",
                LastName = "L"
            };

            RedirectToActionResult result = _controller.Edit(vm).Result as RedirectToActionResult;

            //should redirect to submission id
            Assert.NotNull(result);
            Assert.Equal(_sub.Id, result.RouteValues.Values.FirstOrDefault());

            _sub = _db.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should still be submitted
            Assert.Equal(Status.CommunityMgrReview, _sub.Status);
            
            Assert.Equal("A", _sub.Address);
            Assert.Equal("D", _sub.Description);
            Assert.Equal("E", _sub.Email);
            Assert.Equal("F", _sub.FirstName);
            Assert.Equal("L", _sub.LastName);
        }
    }
}
