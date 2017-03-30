﻿using System;
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
    public class TallyVotesTest
    {
        private TestEmail _email;
        private IFileStore _files;
        private ILogger<SubmissionController> _logger;
        private ApplicationDbContext _db;
        private SubmissionController _controller;
        private Submission _sub;

        void Setup(string username)
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
            var josh = _db.Users.FirstOrDefault(u => u.Email.Equals(username));

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
                Status = Status.CommunityMgrReview,
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

        
        public void TallyVotes(TallyStatus reviewDecision, Status expectedStatus)
        {
            Setup("josh.rozzi@fsresidential.com");
            Assert.Equal(Status.CommunityMgrReview, _sub.Status);

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            TallyVotesViewModel vm = new TallyVotesViewModel()
            {
                SubmissionId = _sub.Id,
                Status = reviewDecision.ToString(),
                Comments = "Tally " + reviewDecision.ToString()
            };

            RedirectToActionResult result = _controller.TallyVotes(vm).Result as RedirectToActionResult;

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
            Assert.Equal(expectedStatus, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal(vm.Comments, _sub.Comments.FirstOrDefault().Comments);
            

            //email to proper spot
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = null;

            if (reviewDecision == TallyStatus.Approved || reviewDecision == TallyStatus.ConditionallyApproved || reviewDecision == TallyStatus.Rejected)
            {
                email = _email.Emails.First(e => e.Recipient.Equals("mellomba0526@gmail.com"));
            }
            else if (reviewDecision == TallyStatus.MissingInformation)
            {
                email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            }
            else if (reviewDecision == TallyStatus.HOAInputRequired)
            {
                email = _email.Emails.First(e => e.Recipient.Equals("mellomba0526@gmail.com"));
            }

            Assert.NotNull(email);
        }

        [Fact]
        public void Tally_Approve()
        {
            TallyVotes(TallyStatus.Approved, Status.HOALiasonReview);
        }

        [Fact]
        public void Tally_Rejected()
        {
            TallyVotes(TallyStatus.Rejected, Status.HOALiasonReview);
        }

        [Fact]
        public void Tally_ConditionallyApproved()
        {
            TallyVotes(TallyStatus.ConditionallyApproved, Status.HOALiasonReview);
        }

        [Fact]
        public void Tally_MissingInformation()
        {
            TallyVotes(TallyStatus.MissingInformation, Status.CommunityMgrReturn);
        }

        [Fact]
        public void Tally_HOAInputRequired()
        {
            TallyVotes(TallyStatus.HOAInputRequired, Status.HOALiasonInput);
        }
    }
}
