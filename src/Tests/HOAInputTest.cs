using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using HOA.Controllers;
using HOA.Model;
using HOA.Model.ViewModel;
using HOA.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public class HOAInputTest : TestBase
    {
        [Fact]
        public void FirstReview()
        {
            Setup("kfinnis@gmail.com");
            _sub.Status = Status.ARBTallyVotes;
            _db.SaveChanges();
            

            RedirectToActionResult result = _controller.GetHOAInput(_sub.Id).Result as RedirectToActionResult;

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

            //Submisison should be moved to HOA input
            Assert.Equal(Status.HOALiasonInput, _sub.Status);
            
            //email to HOA liason
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("mellomba0526@gmail.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void LiasonAddInput()
        {
            Setup("kfinnis@gmail.com");

            _sub.Status = Status.HOALiasonInput;
            _db.SaveChanges();

            CommentsViewModel vm = new CommentsViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "HOA INPUT",
                Submission = null
            };

            RedirectToActionResult result = _controller.LiasonInput(vm).Result as RedirectToActionResult;

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

            //Submisison should have moved to tally
            Assert.Equal(Status.ARBTallyVotes, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("HOA INPUT", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.ARBTallyVotes, change.State);

            //email arb chair
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("kfinnis@gmail.com"));
            Assert.NotNull(email);
        }
    }
}
