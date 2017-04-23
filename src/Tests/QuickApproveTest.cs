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
    public class QuickApproveTest : TestBase
    {
        [Fact]
        public void QuickApprove()
        {
            Setup("josh.rozzi@fsresidential.com");

            QuickApproveViewModel vm = new QuickApproveViewModel
            {
                Files = new List<IFormFile>(),
                SubmissionId = _sub.Id,
                UserFeedback = "emaillll",
                Comments = "CCC"
            };

            RedirectToActionResult result = _controller.QuickApprove(vm).Result as RedirectToActionResult;
            Assert.NotNull(result);

            _sub = _db.Submissions
                .Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should have moved to submitted
            Assert.Equal(Status.Approved, _sub.Status);

            //but no file
            Assert.Null(_sub.ResponseDocumentBlob);

            //history entry
            Assert.Equal(1, _sub.Audits.Count);
            History history = _sub.Audits.FirstOrDefault();
            Assert.True(history.Action.Contains("final response"));

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Comment comment = _sub.Comments.FirstOrDefault();
            Assert.True(comment.Comments.Contains("CCC"));

            //email homeowner
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
            Assert.True(email.Message.Contains("emaillll"));
        }

        [Fact]
        public void QuickApprove_WithDocument()
        {
            Setup("josh.rozzi@fsresidential.com");

            FakeFormFile file = new FakeFormFile();
            file.ContentDisposition = "filename=response.pdf";

            QuickApproveViewModel vm = new QuickApproveViewModel
            {
                Files = new List<IFormFile>() { file },
                SubmissionId = _sub.Id,
                UserFeedback = "emaillll",
                Comments = "CCC"
            };

            RedirectToActionResult result = _controller.QuickApprove(vm).Result as RedirectToActionResult;
            Assert.NotNull(result);

            _sub = _db.Submissions
                .Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == _sub.Id);

            //Submisison should have moved to submitted
            Assert.Equal(Status.Approved, _sub.Status);

            //Has approval doc
            Assert.NotNull(_sub.ResponseDocumentBlob);

            //history entry
            Assert.Equal(1, _sub.Audits.Count);
            History history = _sub.Audits.FirstOrDefault();
            Assert.True(history.Action.Contains("final response"));

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Comment comment = _sub.Comments.FirstOrDefault();
            Assert.True(comment.Comments.Contains("CCC"));

            //email homeowner
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
            Assert.True(email.Message.Contains("emaillll"));
            Assert.Equal("response.pdf", email.Attachment);
        }
    }
}