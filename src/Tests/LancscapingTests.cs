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
    public class LancscapingTests : TestBase
    {
        [Fact]
        public void NotLandscapingRelated()
        {
            Setup("tom.mcclung@verizon.net");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            //final review
            ReviewSubmissionViewModel vm = new ReviewSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "Blarg",
                Submission = null,
                Status = ReviewStatus.Approved.ToString()
            };

            AggregateException ex = Assert.Throws<AggregateException>(() => _controller.Review(vm).Result);
            Assert.NotNull(ex);
            Assert.True(ex.ToString().Contains("NOT landscaping"));

            //Switch to landscaping related
            _sub.LandscapingRelated = true;
            _db.SaveChanges();

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
        }

        [Fact]
        public void EmailIfLandscapingRelated()
        {
            Setup("kfinnis@gmail.com");

            _sub.Status = Status.ARBChairReview;
            _sub.LandscapingRelated = true;
            _db.SaveChanges();

            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = true,
                Comments = "COMMENT",
                Submission = null,
                LandscapingRelated = true
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

            //email ALL reviewers
            Assert.Equal(5, _email.Emails.Count);
        }

        [Fact]
        public void HideReviewOption()
        {
            Setup("tom.mcclung@verizon.net");

            _sub.Status = Status.ARBChairReview;
            _sub.LandscapingRelated = true;
            _db.SaveChanges();

            ViewResult result = _controller.View(_sub.Id) as ViewResult;
            ViewSubmissionViewModel vm = result.Model as ViewSubmissionViewModel;
            Assert.NotNull(vm);
            Assert.Equal(false, vm.HideReviewOption); //review should be an option for landscaping related

            _sub.LandscapingRelated = false;
            _db.SaveChanges();

            //Not an option for non-landscaping items
            result = _controller.View(_sub.Id) as ViewResult;
            vm = result.Model as ViewSubmissionViewModel;
            Assert.NotNull(vm);
            Assert.Equal(true, vm.HideReviewOption);
        }
    }
}
