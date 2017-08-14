using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HOA.Model;
using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public class QuorumTests : TestBase
    {
        [Fact]
        public void Quorum()
        {
            Setup("josh.rozzi@fsresidential.com");
            _sub.Status = Status.CommitteeReview;
            _sub.StatusChangeTime = DateTime.Now.AddDays(-10);
            _db.SaveChanges();

            var sergio = _db.Users.FirstOrDefault(u => u.Email.Equals("sergio.carrillo@alumni.duke.edu"));
            var deana = _db.Users.FirstOrDefault(u => u.Email.Equals("deanaclymer@verizon.net"));

            //Add one review
            var r1 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = sergio,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };
            
            _sub.Reviews.Add(r1);
            _db.Reviews.Add(r1);
            _db.SaveChanges();

            _reminder.CheckQuorum();

            _sub = _db.Submissions
                .Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == _sub.Id);

            //Should NOT have reached quorum
            Assert.Equal(0, _email.Emails.Count);
            Assert.Equal(Status.CommitteeReview, _sub.Status);

            //Cross quorum line
            var r2 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = deana,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };
            _sub.Reviews.Add(r2);
            _db.Reviews.Add(r2);
            _db.SaveChanges();

            _reminder.CheckQuorum();
            Assert.NotEqual(0, _email.Emails.Count);
            Assert.Equal(Status.ARBTallyVotes, _sub.Status);
        }

        [Fact]
        public void Quorum_WithLandscaping()
        {
            Setup("josh.rozzi@fsresidential.com");
            _sub.Status = Status.CommitteeReview;
            _sub.StatusChangeTime = DateTime.Now.AddDays(-10);
            _sub.LandscapingRelated = true;
            _db.SaveChanges();

            var sergio = _db.Users.FirstOrDefault(u => u.Email.Equals("sergio.carrillo@alumni.duke.edu"));
            var deana = _db.Users.FirstOrDefault(u => u.Email.Equals("deanaclymer@verizon.net"));
            var tom = _db.Users.FirstOrDefault(u => u.Email.Equals("tom.mcclung@verizon.net"));

            //Add two reviews
            var r1 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = sergio,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };
            _sub.Reviews.Add(r1);
            _db.Reviews.Add(r1);

            var r2 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = deana,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };
            _sub.Reviews.Add(r2);
            _db.Reviews.Add(r2);
            
            _db.SaveChanges();

            _reminder.CheckQuorum();

            _sub = _db.Submissions
                .Include(s => s.Reviews)
                .Include(s => s.Audits)
                .Include(s => s.Responses)
                .Include(s => s.Files)
                .Include(s => s.StateHistory)
                .Include(s => s.Comments)
                .FirstOrDefault(s => s.Id == _sub.Id);

            //Should NOT have reached quorum (due to higher requirement for landscaping)
            Assert.Equal(0, _email.Emails.Count);
            Assert.Equal(Status.CommitteeReview, _sub.Status);

            //Cross quorum line
            var r3 = new Review()
            {
                Comments = "Rev1",
                Created = DateTime.Now,
                Reviewer = tom,
                Status = ReviewStatus.Approved,
                SubmissionRevision = 1
            };
            _sub.Reviews.Add(r3);
            _db.Reviews.Add(r3);
            _db.SaveChanges();

            _reminder.CheckQuorum();
            Assert.NotEqual(0, _email.Emails.Count);
            Assert.Equal(Status.ARBTallyVotes, _sub.Status);
        }
    }
}
