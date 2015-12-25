using HOA.Model;
using HOA.Services;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Util
{
    public static class EmailHelper
    {
        private static string m_baseUrl = "http://hoaweb1.azurewebsites.net/";

        private static string m_availableEmail = @"
A new submission is available for action:<br>
<br>
<a href='{0}'>{0}</a><br>
Status: {1}<br>";

        private static string m_overdueEmail = @"
The following submission is overdue:<br>
<br>
<a href='{0}'>{0}</a><br>
Status: {1}<br>";

        private static string m_returnedEmail = @"
{0} {1},<br>
<br>
Your submission {2}. You can use the link below to view your submission and any comments.<br>
<br>
<a href='{3}'>{3}</a><br>";

        public static void NotifyStatusChanged(ApplicationDbContext context, Submission submission, IEmailSender mail)
        {
            if (submission.Status == Status.Approved || 
                submission.Status == Status.Rejected || 
                submission.Status == Status.MissingInformation || 
                submission.Status == Status.ConditionallyApproved)
            {
                NotifyHomeowner(context, submission, mail);
                return;
            }

            string roleToNofity;
            if (submission.Status == Status.Submitted)
            {
                roleToNofity = RoleNames.CommunityManager;
            }
            else if (submission.Status == Status.ARBIncoming)
            {
                roleToNofity = RoleNames.BoardChairman;
            }
            else if (submission.Status == Status.UnderReview)
            {
                roleToNofity = RoleNames.BoardMember;
            }
            else if (submission.Status == Status.ARBFinal)
            {
                roleToNofity = RoleNames.BoardChairman;
            }
            else if (submission.Status == Status.ReviewComplete)
            {
                roleToNofity = RoleNames.HOALiaison;
            }
            else if (submission.Status == Status.PrepConditionalApproval || submission.Status == Status.PrepApproval)
            {
                roleToNofity = RoleNames.CommunityManager;
            }
            else if (submission.Status == Status.Retracted)
            {
                roleToNofity = RoleNames.CommunityManager;
            }
            else
            {
                throw new Exception("Unknown status");
            }

            List<string> emails = GetRoleMembers(context, roleToNofity);

            var link = String.Format("{0}Submission/View/{1}", m_baseUrl, submission.Id);
            var emailHtml = String.Format(m_availableEmail, link, submission.Status.ToString());
            mail.SendEmailAsync(emails, "ARB: New submission", emailHtml);
        }

        private static List<String> GetRoleMembers(ApplicationDbContext context, string roleName)
        {
            var role = context.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(roleName));
            List<string> userIds = role.Users.Select(u => u.UserId).ToList();
            return context.Users.Where(u => userIds.Contains(u.Id) && u.Enabled).Select(u => u.Email).ToList();
        }

        private static void NotifyHomeowner(ApplicationDbContext context, Submission submission, IEmailSender mail)
        {
            var subject = String.Format("ARB Submission {0}", submission.Code);

            string status;
            if (submission.Status == Status.Approved)
            {
                status = "has been approved";
            }
            else if (submission.Status == Status.ConditionallyApproved)
            {
                status = "has been conditionally approved";
            }
            else if (submission.Status == Status.MissingInformation)
            {
                status = "has been returned due to missing information";
            }
            else
            {
                status = "has been rejected";
            }

            var link = String.Format("{0}Submission/ViewStatus/{1}", m_baseUrl, submission.Code);
            string emailHtml = String.Format(m_returnedEmail, submission.FirstName, submission.LastName, status, link);
            mail.SendEmailAsync(new List<string> { submission.Email }, "ARB: New submission", emailHtml);
        }

        public static void NotifySubmissonOverdue(ApplicationDbContext context, Submission submission, IEmailSender mail)
        {
            string roleToNofity = null;
            List<string> emails;
            if (submission.Status == Status.Submitted)
            {
                roleToNofity = RoleNames.CommunityManager;
            }
            else if (submission.Status == Status.ARBIncoming)
            {
                roleToNofity = RoleNames.BoardChairman;
            }
            else if (submission.Status == Status.UnderReview)
            {
            }
            else if (submission.Status == Status.ARBFinal)
            {
                roleToNofity = RoleNames.BoardChairman;
            }
            else if (submission.Status == Status.ReviewComplete)
            {
                roleToNofity = RoleNames.HOALiaison;
            }
            else
            {
                throw new Exception("Unknown status");
            }


            if (submission.Status == Status.UnderReview)
            {
                var role = context.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(RoleNames.BoardMember));
                List<string> userIds = role.Users.Select(u => u.UserId).ToList();
                var board = context.Users.Where(u => userIds.Contains(u.Id) && u.Enabled).Select(u => u.Id).ToList();
                var alreadyReviewed = context.Reviews.Where(r => r.Submission.Id == submission.Id).Select(r => r.Reviewer.Id).ToList();
                var toReview = board.Except(alreadyReviewed);
                emails = context.Users.Where(u => toReview.Contains(u.Id) && u.Enabled).Select(u => u.Email).ToList();
            }
            else
            {
                emails = GetRoleMembers(context, roleToNofity);
            }

            var link = String.Format("{0}Submission/View/{1}", m_baseUrl, submission.Id);
            var emailHtml = String.Format(m_overdueEmail, link, submission.Status.ToString());
            mail.SendEmailAsync(emails, "ARB: Overdue submission", emailHtml);
        }
    }
}
