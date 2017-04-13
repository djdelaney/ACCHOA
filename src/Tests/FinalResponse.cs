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
    public class FinalResponse : TestBase
    {
        [Fact]
        public void FinalResponse_Approve()
        {
            Setup("josh.rozzi@fsresidential.com");

            _sub.Status = Status.FinalResponse;
            _sub.ReturnStatus = ReturnStatus.Approved;
            _db.SaveChanges();

            FinalResponseViewModel vm = new FinalResponseViewModel()
            {
                SubmissionId = _sub.Id,
                Comments = "COMMENT3",
                Submission = null,
                UserFeedback = "to user"
            };

            RedirectToActionResult result = _controller.FinalResponse(vm).Result as RedirectToActionResult;

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

            //Move to approved
            Assert.Equal(Status.Approved, _sub.Status);

            //internal comment
            Assert.Equal(1, _sub.Comments.Count);
            Assert.Equal("COMMENT3", _sub.Comments.FirstOrDefault().Comments);

            //No state switch for final
            Assert.Equal(0, _sub.StateHistory.Count);

            //email to homeowner
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
        }
    }
}
