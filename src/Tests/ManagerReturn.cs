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
    public class ManagerReturn : TestBase
    {
        [Fact]
        public void Return_MissingInfo()
        {
            Setup("josh.rozzi@fsresidential.com");

            _sub.Status = Status.CommunityMgrReturn;
            _sub.ReturnStatus = ReturnStatus.MissingInformation;
            _db.SaveChanges();

            ReturnCommentsViewModel vm = new ReturnCommentsViewModel()
            {
                SubmissionId = _sub.Id,
                UserFeedback = "blah"
            };

            RedirectToActionResult result = _controller.CommunityMgrReturn(vm).Result as RedirectToActionResult;

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

            //Submisison should be marked missing info
            Assert.Equal(ReturnStatus.MissingInformation, _sub.ReturnStatus);

            //internal comment
            Assert.Equal(0, _sub.Comments.Count);

            //history entry
            Assert.Equal(1, _sub.StateHistory.Count);
            StateChange change = _sub.StateHistory.FirstOrDefault();
            Assert.Equal(Status.MissingInformation, change.State);

            //email to homeowner
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("Test@gmail.com"));
            Assert.NotNull(email);
        }
    }
}
