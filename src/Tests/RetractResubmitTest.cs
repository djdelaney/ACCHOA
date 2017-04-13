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
    public class RetractResubmitTest : TestBase
    {
        [Fact]
        public void Retract_Existing()
        {
            Setup("kfinnis@gmail.com");

            RedirectToActionResult result = _controller.Retract(_sub.Id) as RedirectToActionResult;
            Assert.NotNull(result);
            
            _sub = _db.Submissions
                    .Include(s => s.Reviews)
                    .Include(s => s.Audits)
                    .Include(s => s.Responses)
                    .Include(s => s.Files)
                    .Include(s => s.StateHistory)
                    .Include(s => s.Comments)
                    .FirstOrDefault(s => s.Id == _sub.Id);

            //Should now be retracted
            Assert.Equal(Status.Retracted, _sub.Status);
        }

        [Fact]
        public void Resubmit()
        {
            Setup("kfinnis@gmail.com");

            _sub.Status = Status.Rejected;
            _db.SaveChanges();

            ResubmitViewModel vm = new ResubmitViewModel
            {
                Description = "NEW desc",
                Files = new List<IFormFile>(),
                SubmissionId = _sub.Id
            };

            RedirectToActionResult result = _controller.Resubmit(vm).Result as RedirectToActionResult;
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
            Assert.Equal(Status.CommunityMgrReview, _sub.Status);
            
            //comment entry
            Assert.Equal(1, _sub.Audits.Count);
            History history = _sub.Audits.FirstOrDefault();
            Assert.True(history.Action.Contains("Resubmit"));

            //email community mgr, and homeowner
            Assert.Equal(2, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("josh.rozzi@fsresidential.com"));
            Assert.NotNull(email);
            email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
        }
    }
}
