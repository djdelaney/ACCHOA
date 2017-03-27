using System;
using System.Collections.Generic;
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
using Microsoft.EntityFrameworkCore;
using Tests.Helpers;

namespace Tests
{
    public class SubmissionCreationTester
    {
        private TestEmail _email;
        private IFileStore _files;
        private ILogger<SubmissionController> _logger;
        private ApplicationDbContext _db;
        private SubmissionController _controller;

        public void Setup()
        {
            _email = new TestEmail();
            _files = new FileMock();
            _logger = new MockLogging<SubmissionController>();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase();

            _db = new ApplicationDbContext(optionsBuilder.Options);
            _db.Database.EnsureDeleted();
            _db.Database.EnsureCreated();
            SampleTestData.SetupUsersAndRoles(_db);

            // Setup
            var mockUsers = MockHelpers.MockUserManager<ApplicationUser>();
            var user = mockUsers.Object;

            var mockRoles = MockHelpers.MockRoleManager<IdentityRole>();
            var role = mockRoles.Object;

            _controller = new SubmissionController(_db, user, _email, _files, role, _logger);
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
        public void Submission_Success()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext =  new DefaultHttpContext() { User = mockPrincipal.Object };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.OpenReadStream()).Returns(Stream.Null);
            fileMock.Setup(m => m.ContentDisposition).Returns("filename=AAA.pdf");
            fileMock.Setup(m => m.Length).Returns(1024);

            CreateSubmissionViewModel vm = new CreateSubmissionViewModel()
            {
                Address = "241 Sills Ln",
                Description = "Deck",
                Email = "person@gmail.com",
                Files = new List<IFormFile>() { fileMock.Object },
                FirstName = "Dan",
                LastName = "DaMan"
            };

            Microsoft.AspNetCore.Mvc.ViewResult result = _controller.Create(vm).Result as Microsoft.AspNetCore.Mvc.ViewResult;
            Assert.NotNull(result);

            //Results
            Submission sub = result.Model as Submission;
            Assert.NotNull(sub);
            Assert.Equal(1, _db.Submissions.ToList().Count);
            Assert.Equal(Status.CommunityMgrReview, sub.Status);

            //Assert incoming email sent to josh, and homeowner
            Assert.Equal(2, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            Assert.NotNull(email);

            email = _email.Emails.First(e => e.Recipient.Equals("person@gmail.com"));
            Assert.NotNull(email);
        }

        [Fact]
        public void Submission_FileTooLarge()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.OpenReadStream()).Returns(Stream.Null);
            fileMock.Setup(m => m.ContentDisposition).Returns("filename=AAA.pdf");
            fileMock.Setup(m => m.Length).Returns(1024*1024*20); //20MB

            CreateSubmissionViewModel vm = new CreateSubmissionViewModel()
            {
                Address = "241 Sills Ln",
                Description = "Deck",
                Email = "person@gmail.com",
                Files = new List<IFormFile>() { fileMock.Object },
                FirstName = "Dan",
                LastName = "DaMan"
            };

            Microsoft.AspNetCore.Mvc.ViewResult result = _controller.Create(vm).Result as Microsoft.AspNetCore.Mvc.ViewResult;
            Assert.NotNull(result);
            Assert.Equal(false, result.ViewData.ModelState.IsValid);
            Assert.Equal(1, result.ViewData.ModelState.ErrorCount);
            Assert.True(result.ViewData.ModelState.Values.FirstOrDefault().Errors.FirstOrDefault().ErrorMessage.Contains("Files too large"));
        }

        [Fact]
        public void Submission_InvalidFileType()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(m => m.OpenReadStream()).Returns(Stream.Null);
            fileMock.Setup(m => m.ContentDisposition).Returns("filename=AAA.exe");
            fileMock.Setup(m => m.Length).Returns(1024 * 1024);

            CreateSubmissionViewModel vm = new CreateSubmissionViewModel()
            {
                Address = "241 Sills Ln",
                Description = "Deck",
                Email = "person@gmail.com",
                Files = new List<IFormFile>() { fileMock.Object },
                FirstName = "Dan",
                LastName = "DaMan"
            };

            Microsoft.AspNetCore.Mvc.ViewResult result = _controller.Create(vm).Result as Microsoft.AspNetCore.Mvc.ViewResult;
            Assert.NotNull(result);
            Assert.Equal(false, result.ViewData.ModelState.IsValid);
            Assert.Equal(1, result.ViewData.ModelState.ErrorCount);
            Assert.True(result.ViewData.ModelState.Values.FirstOrDefault().Errors.FirstOrDefault().ErrorMessage.Contains("Invalid file type"));
        }

        [Fact]
        public void Submission_FileRequired()
        {
            Setup();

            //Current user mock
            var mockPrincipal = GetMockUser();
            _controller.ControllerContext.HttpContext = new DefaultHttpContext() { User = mockPrincipal.Object };

            CreateSubmissionViewModel vm = new CreateSubmissionViewModel()
            {
                Address = "241 Sills Ln",
                Description = "Deck",
                Email = "person@gmail.com",
                Files = new List<IFormFile>() {  },
                FirstName = "Dan",
                LastName = "DaMan"
            };

            Microsoft.AspNetCore.Mvc.ViewResult result = _controller.Create(vm).Result as Microsoft.AspNetCore.Mvc.ViewResult;
            Assert.NotNull(result);
            Assert.Equal(false, result.ViewData.ModelState.IsValid);
            Assert.Equal(1, result.ViewData.ModelState.ErrorCount);
            Assert.True(result.ViewData.ModelState.Values.FirstOrDefault().Errors.FirstOrDefault().ErrorMessage.Contains("Files are required"));
        }
    }
}
