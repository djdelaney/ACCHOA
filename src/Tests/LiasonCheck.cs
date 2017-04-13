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
    public class LiasonCheck : TestBase
    {
        [Fact]
        public void Liason_Approve()
        {
            Setup("mellomba0526@gmail.com");
            
            FinalReview vm = new FinalReview()
            {
                SubmissionId = _sub.Id,
                Status = ReturnStatus.Approved.ToString(),
                Comments = "COMMENT3"
            };

            RedirectToActionResult result = _controller.FinalCheck(vm).Result as RedirectToActionResult;

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

            //Submisison should be marked for return, to be approved
            Assert.Equal(Status.FinalResponse, _sub.Status);
            Assert.Equal(ReturnStatus.Approved, _sub.ReturnStatus);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT3", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.FinalResponse, change.State);

            //email to ARB chair
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void Liason_MissingInfo()
        {
            Setup("mellomba0526@gmail.com");

            FinalReview vm = new FinalReview()
            {
                SubmissionId = _sub.Id,
                Status = ReturnStatus.MissingInformation.ToString(),
                Comments = "COMMENT3"
            };

            RedirectToActionResult result = _controller.FinalCheck(vm).Result as RedirectToActionResult;

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

            //Submisison should be marked for return, to be marked missing info
            Assert.Equal(Status.CommunityMgrReturn, _sub.Status);
            Assert.Equal(ReturnStatus.MissingInformation, _sub.ReturnStatus);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT3", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.CommunityMgrReturn, change.State);

            //email to ARB chair
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            Assert.NotNull(email);
        }
    }
}
