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
    public class CompletionCheck
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
            var josh = _db.Users.FirstOrDefault(u => u.Email.Equals("josh.rozzi@fsresidential.com"));

            mockUsers.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).Returns(Task.FromResult<HOA.Model.ApplicationUser>(josh));
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
                Status = Status.Submitted,
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
        public void Submitted_Approve()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };
            
            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = true,
                Comments = "COMMENT",
                Submission = null,
                UserFeedback = "asdas"
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

            //Submisison should have moved to ARB incoming
            Assert.Equal(Status.ARBIncoming, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.ARBIncoming, change.State);

            //email to ARB chair
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("kfinnis@gmail.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void ARBIncoming_Approve()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            _sub.Status = Status.ARBIncoming;
            _db.SaveChanges();

            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = true,
                Comments = "COMMENT",
                Submission = null,
                UserFeedback = "asdas"
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

            //Submisison should have moved to ARB incoming
            Assert.Equal(Status.UnderReview, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.UnderReview, change.State);

            //email reviewers
            Assert.Equal(3, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("dletscher@brenntag.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void MissingInformation()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            _sub.Status = Status.ARBIncoming;
            _db.SaveChanges();

            CheckCompletenessViewModel vm = new CheckCompletenessViewModel()
            {
                SubmissionId = _sub.Id,
                Approve = false,
                Comments = "MissingInfo",
                Submission = null,
                UserFeedback = "asdas"
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

            //Submisison should have moved to ARB incoming
            Assert.Equal(Status.MissingInformation, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("MissingInfo", _sub.Comments.FirstOrDefault().Comments);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.MissingInformation, change.State);

            //email reviewers
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
        }
    }
}
