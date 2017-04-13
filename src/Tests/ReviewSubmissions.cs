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
    public class ReviewSubmissions : TestBase
    {
        [Fact]
        public void FirstReview()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            ReviewSubmissionViewModel vm = new ReviewSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "COMMENT",
                Submission = null,
                Status = "Approved"
            };

            RedirectToActionResult result = _controller.Review(vm).Result as RedirectToActionResult;

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
            
            //Submisison should still be under review
            Assert.Equal(Status.CommitteeReview, _sub.Status);

            //Single review
            Assert.Equal(1, _sub.Reviews.Count);
            Assert.Equal("COMMENT", _sub.Reviews.FirstOrDefault().Comments);
            Assert.Equal(ReviewStatus.Approved, _sub.Reviews.FirstOrDefault().Status);
            
            //email to ARB chair
            Assert.Equal(0, _email.Emails.Count);
        }

        [Fact]
        public void FinalReview()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            var sergio = _db.Users.FirstOrDefault(u => u.Email.Equals("sergio.carrillo@alumni.duke.edu"));
            var deana = _db.Users.FirstOrDefault(u => u.Email.Equals("deanaclymer@verizon.net"));

            //Add two reviews
            var r1 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = sergio,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };

            var r2 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = deana,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };

            _sub.Reviews.Add(r1);
            _sub.Reviews.Add(r2);
            _db.Reviews.Add(r1);
            _db.Reviews.Add(r2);
            _db.SaveChanges();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            ReviewSubmissionViewModel vm = new ReviewSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "COMMENT",
                Submission = null,
                Status = "Approved"
            };

            RedirectToActionResult result = _controller.Review(vm).Result as RedirectToActionResult;

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

            //Submisison should be finished reviewing
            Assert.Equal(Status.ARBTallyVotes, _sub.Status);

            //Single review
            Assert.Equal(3, _sub.Reviews.Count);

            //email to ARB chair
            Assert.Equal(1, _email.Emails.Count);
        }
    }
}
