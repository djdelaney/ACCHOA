using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using HOA.Model;
using Tests.Helpers;
using Xunit;

namespace Tests
{
    public class ReminderTests : TestBase
    {
        [Fact]
        public void NoReminders()
        {
            Setup("josh.rozzi@fsresidential.com");

            List<Status> statuses = new EditableList<Status>()
            {
                Status.CommunityMgrReview,
                Status.ARBChairReview,
                Status.CommitteeReview,
                Status.ARBTallyVotes,
                Status.HOALiasonReview,
                Status.FinalResponse,
                Status.CommunityMgrReturn,
                Status.HOALiasonInput,
            };

            foreach (var stat in statuses)
            {
                _sub.Status = stat;
                _db.SaveChanges();

                _reminder.Process();
                Assert.Equal(0, _email.Emails.Count);
            }
        }

        [Fact]
        public void Overdue()
        {
            Setup("josh.rozzi@fsresidential.com");
            _sub.StatusChangeTime = DateTime.Now.AddDays(-10);

            List<Status> statuses = new EditableList<Status>()
            {
                Status.CommunityMgrReview,
                Status.ARBChairReview,
                Status.CommitteeReview,
                Status.ARBTallyVotes,
                Status.HOALiasonReview,
                Status.FinalResponse,
                Status.CommunityMgrReturn,
                Status.HOALiasonInput,
            };

            foreach (var stat in statuses)
            {
                _sub.Status = stat;
                _db.SaveChanges();

                _reminder.Process();

                if(stat == Status.CommitteeReview)
                    Assert.Equal(3, _email.Emails.Count);
                else
                    Assert.Equal(1, _email.Emails.Count);

                _email.Emails.Clear();
            }
        }


        [Fact]
        public void OnlyUnsubmittedReviews()
        {
            Setup("josh.rozzi@fsresidential.com");
            _sub.Status = Status.CommitteeReview;
            _sub.StatusChangeTime = DateTime.Now.AddDays(-10);
            _db.SaveChanges();

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

            _reminder.Process();

            //Only send email to missing reviews
            Assert.Equal(1, _email.Emails.Count);
            TestEmail.Email email = _email.Emails.First(e => e.Recipient.Equals("dletscher@brenntag.com"));
            Assert.NotNull(email);
        }
    }
}
