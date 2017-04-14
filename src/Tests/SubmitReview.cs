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
    public class SubmitReview : TestBase
    {
        private void CheckReviewStatus(ReviewStatus status, bool commentsRequired, string comment)
        {
            ReviewSubmissionViewModel vm = new ReviewSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = comment,
                Submission = null,
                Status = status.ToString()
            };

            if (commentsRequired && string.IsNullOrEmpty(comment))
            {
                var res = _controller.Review(vm).Result;
                Microsoft.AspNetCore.Mvc.ViewResult errResult = res as Microsoft.AspNetCore.Mvc.ViewResult;
                Assert.NotNull(errResult);
                Assert.Equal(false, errResult.ViewData.ModelState.IsValid);
                Assert.Equal(1, errResult.ViewData.ModelState.ErrorCount);
                Assert.True(errResult.ViewData.ModelState.Values.FirstOrDefault().Errors.FirstOrDefault().ErrorMessage.Contains("must supply comments"));
                return;
            }

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

            //No coments, statue, or email for reviews
            Assert.Equal(0, _sub.Comments.Count);
            Assert.Equal(0, _sub.StateHistory.Count);
            Assert.Equal(0, _email.Emails.Count);

            //Review
            Assert.Equal(1, _sub.Reviews.Count);
            Review r = _sub.Reviews.First();
            Assert.Equal(status, r.Status);
            Assert.Equal(comment, r.Comments);
        }

        [Fact]
        public void Review_Approve()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.Approved, false, null);
        }

        [Fact]
        public void Review_Approve_Comments()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.Approved, false, "Approve!");
        }

        [Fact]
        public void Review_Reject()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.Rejected, true, null);
        }

        [Fact]
        public void Review_Reject_Comments()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.Rejected, true, "REJECT!");
        }

        [Fact]
        public void Review_ConditionallyApproved()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.ConditionallyApproved, true, null);
        }

        [Fact]
        public void Review_ConditionallyApproved_Comments()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.ConditionallyApproved, true, "REJECT!");
        }

        [Fact]
        public void Review_MissingInformation()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.MissingInformation, true, null);
        }

        [Fact]
        public void Review_MissingInformation_Comments()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.MissingInformation, true, "REJECT!");
        }

        [Fact]
        public void Review_Abstain()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.Abstain, true, null);
        }

        [Fact]
        public void Review_Abstain_Comments()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();
            CheckReviewStatus(ReviewStatus.Abstain, true, "REJECT!");
        }

        [Fact]
        public void AlreadyReviewed()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            ReviewSubmissionViewModel vm = new ReviewSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "Comment1",
                Submission = null,
                Status = ReviewStatus.Approved.ToString()
            };

            RedirectToActionResult result = _controller.Review(vm).Result as RedirectToActionResult;
            Assert.NotNull(result);

            //Try reviewing again
            AggregateException ex = Assert.Throws<AggregateException>(() => _controller.Review(vm).Result);
            Assert.NotNull(ex);
            Assert.True(ex.ToString().Contains("Already reviewed!"));
        }


        [Fact]
        public void FinalReview()
        {
            Setup("dletscher@brenntag.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            ApplicationUser deana = _db.Users.FirstOrDefault(u => u.Email.Equals("deanaclymer@verizon.net"));
            ApplicationUser sergio = _db.Users.FirstOrDefault(u => u.Email.Equals("sergio.carrillo@alumni.duke.edu"));

            var review1 = new Review()
                {
                Comments = "1",
                Created = DateTime.Now,
                Status = ReviewStatus.Approved,
                Submission = _sub,
                SubmissionRevision = _sub.Revision,
                Reviewer = deana
                };

            var review2 = new Review()
            {
                Comments = "2",
                Created = DateTime.Now,
                Status = ReviewStatus.Approved,
                Submission = _sub,
                SubmissionRevision = _sub.Revision,
                Reviewer = sergio
            };

            _sub.Reviews.Add(review1);
            _sub.Reviews.Add(review2);
            _db.SaveChanges();

            //final review
            ReviewSubmissionViewModel vm = new ReviewSubmissionViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "Final",
                Submission = null,
                Status = ReviewStatus.Approved.ToString()
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

            //Submisison should be moved to tallying votes
            Assert.Equal(Status.ARBTallyVotes, _sub.Status);

            //email chairman for tallying votes
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("kfinnis@gmail.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void SkipQuorum()
        {
            Setup("kfinnis@gmail.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            ApplicationUser deana = _db.Users.FirstOrDefault(u => u.Email.Equals("deanaclymer@verizon.net"));
            ApplicationUser sergio = _db.Users.FirstOrDefault(u => u.Email.Equals("sergio.carrillo@alumni.duke.edu"));

            var review1 = new Review()
            {
                Comments = "1",
                Created = DateTime.Now,
                Status = ReviewStatus.Approved,
                Submission = _sub,
                SubmissionRevision = _sub.Revision,
                Reviewer = deana
            };

            var review2 = new Review()
            {
                Comments = "2",
                Created = DateTime.Now,
                Status = ReviewStatus.Approved,
                Submission = _sub,
                SubmissionRevision = _sub.Revision,
                Reviewer = sergio
            };

            _sub.Reviews.Add(review1);
            _sub.Reviews.Add(review2);
            _db.SaveChanges();
            
            RedirectToActionResult result = _controller.SkipQuorum(_sub.Id).Result as RedirectToActionResult;

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

            //Submisison should be moved to tallying votes
            Assert.Equal(Status.ARBTallyVotes, _sub.Status);

            //email chairman for tallying votes
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("kfinnis@gmail.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void PrecedentSetting()
        {
            Setup("kfinnis@gmail.com");
            _sub.Status = Status.CommitteeReview;
            _db.SaveChanges();

            RedirectToActionResult result = _controller.PrecedentSetting(_sub.Id).Result as RedirectToActionResult;

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

            //email homeowner
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
        }
    }
}
