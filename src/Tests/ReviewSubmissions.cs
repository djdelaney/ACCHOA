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
{/*
    public class ReviewSubmissions
    {
        private TestEmail _email;
        private IFileStore _files;
        private ILogger<SubmissionController> _logger;
        private ApplicationDbContext _db;
        private SubmissionController _controller;
        private Submission _sub;

        public void Setup()
        {
            _email = new TestEmail();
            _files = new FileMock();
            _logger = new MockLogging<SubmissionController>();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase();

            _db = new ApplicationDbContext(optionsBuilder.Options);
            _db.Database.EnsureCreated();
            SampleTestData.SetupUsersAndRoles(_db);

            // Setup
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mockUsers = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            var dan = _db.Users.FirstOrDefault(u => u.Email.Equals("dletscher@brenntag.com"));

            mockUsers.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).Returns(Task.FromResult<HOA.Model.ApplicationUser>(dan));
            var user = mockUsers.Object;

            var mockRoles = MockHelpers.MockRoleManager<IdentityRole>();
            var role = mockRoles.Object;

            _controller = new SubmissionController(_db, user, _email, _files, role, _logger);

            _sub = new Submission()
            {
                FirstName = "Joe",
                LastName = "Smith",
                Address = "123 Address",
                Email = "Test@gmail.com",
                Description = "Deck",
                Status = Status.UnderReview,
                StatusChangeTime = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                SubmissionDate = DateTime.UtcNow.AddHours(-1),
                Code = "ABC123",
                Reviews = new List<Review>(),
                Audits = new List<History>(),
                Responses = new List<Response>(),
                Files = new List<HOA.Model.File>(),
                StateHistory = new List<StateChange>(),
                Comments = new List<Comment>(),
                Revision = 1,
                PrecedentSetting = false
            };
            _db.Submissions.Add(_sub);
            _db.SaveChanges();
        }

        private Mock<ClaimsPrincipal> GetMockUser()
        {
            var username = "FakeUserName";
            var identity = new GenericIdentity(username, "");

            var mockPrincipal = new Mock<ClaimsPrincipal>();
            mockPrincipal.Setup(x => x.Identity).Returns(identity);
            mockPrincipal.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(true);
            mockPrincipal.Setup(x => x.HasClaim(It.IsAny<Predicate<Claim>>())).Returns(true);

            return mockPrincipal;
        }

        [Fact]
        public void FirstReview()
        {
            Setup();

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
            Assert.Equal(Status.UnderReview, _sub.Status);

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
            Setup();

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

            //Submisison should still be under review
            Assert.Equal(Status.ARBFinal, _sub.Status);

            //Single review
            Assert.Equal(3, _sub.Reviews.Count);

            //email to ARB chair
            Assert.Equal(1, _email.Emails.Count);
        }
    }*/
}
