using HOA.Model;
using HOA.Services;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Util
{
    public static class EmailHelper
    {
        public static string BaseHost = "";

        private static string m_newAccount = @"
Welcome to the Applecross ARB website!<br>
<br>
Your new username is: {0}<br>
and password: {1}<br>
<br>
Please use the link below to login and change your password:<br>
<a href='{2}'>{2}</a><br>
";

        private static string m_availableEmail = @"
A new submission is available for action:<br>
<br>
<a href='{0}'>{0}</a><br>
Status: {1}<br>";

        private static string m_overdueEmail = @"
The following submission(s) are overdue:<br>
<br>
{0}";

        private static string m_homeownerEmail = @"
{0} {1},<br>
<br>
Your submission {2}. Your access code to check your submission status is {3}. Use the link below to view your submission and any comments.<br>
<br>
<a href='{4}'>{4}</a><br>";

        private static string m_homeownerFinalEmail = @"
{0} {1},<br>
<br>
{2}<br>
<br>
<a href='{3}'>{3}</a><br>";

        private static string m_PrecedentSettingEmail = @"
{0} {1},<br>
<br>
Your submission has been marked as precedent setting by the review board and may take longer than usual to review. Use the link below to view your submission and any comments.<br>
<br>
<a href='{2}'>{2}</a><br>";

        private static string m_ResetPasswordEmail = @"
Please reset your password by clicking here:<br>
<br>
<a href='{0}'>{0}</a><br>";

        public static void NotifyResetPassword(string email, string link, IEmailSender mail)
        {
            link = String.Format("{0}{1}", BaseHost, link);
            string emailHtml = String.Format(m_ResetPasswordEmail, link);
            mail.SendEmailAsync(new List<string> { email }, "ARB: Forgot Password", emailHtml, null, null);
        }

        public static void NotifyFinalResponse(ApplicationDbContext context, Submission submission, string comments, IEmailSender mail, Stream file, string attachmentName)
        {
            var subject = String.Format("ARB Submission {0}", submission.Code);
            string status = GetStatusText(submission.Status);
            var link = String.Format("{0}/Submission/ViewStatus/{1}", BaseHost, submission.Code);
            string emailHtml = String.Format(m_homeownerFinalEmail, submission.FirstName, submission.LastName, comments, link);
            mail.SendEmailAsync(new List<string> { submission.Email }, subject, emailHtml, file, attachmentName);
        }

        public static void NotifyPrecedentSetting(ApplicationDbContext context, Submission submission, IEmailSender mail)
        {
            var link = String.Format("{0}/Submission/ViewStatus/{1}", BaseHost, submission.Code);
            string emailHtml = String.Format(m_PrecedentSettingEmail, submission.FirstName, submission.LastName, link);
            mail.SendEmailAsync(new List<string> { submission.Email }, string.Format("ARB: New submission {0}", submission.Code), emailHtml, null, null);
        }

        public static void NotifyStatusChanged(ApplicationDbContext context, Submission submission, IEmailSender mail, Stream file = null, string attachmentName = null)
        {
            if (submission.Status == Status.Approved || 
                submission.Status == Status.Rejected || 
                submission.Status == Status.MissingInformation || 
                submission.Status == Status.ConditionallyApproved)
            {
                NotifyHomeowner(context, submission, mail, file, attachmentName);
                return;
            }

            string roleToNofity;
            if (submission.Status == Status.Submitted)
            {
                NotifyHomeowner(context, submission, mail, file, attachmentName);
                roleToNofity = RoleNames.CommunityManager;
            }
            else if (submission.Status == Status.ARBIncoming)
            {
                roleToNofity = RoleNames.BoardChairman;
            }
            else if (submission.Status == Status.UnderReview)
            {
                roleToNofity = RoleNames.ARBBoardMember;
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
            if (emails == null || emails.Count() == 0)
                return;

            var link = String.Format("{0}/Submission/View/{1}", BaseHost, submission.Id);
            var emailHtml = String.Format(m_availableEmail, link, submission.Status.ToString());
            mail.SendEmailAsync(emails, string.Format("ARB: Available for processing {0}", submission.Code), emailHtml, null, null);
        }

        public static void NotifyNewUser(string email, string password, IEmailSender mail)
        {
            var link = String.Format("{0}/Account/Login/?returnUrl=/Account/ChangePassword/", BaseHost);
            string emailHtml = string.Format(m_newAccount, email, password, link);
            mail.SendEmailAsync(new List<string> { email }, "ACC ARB: New Account", emailHtml, null, null);
        }

        private static List<String> GetRoleMembers(ApplicationDbContext context, string roleName)
        {
            var role = context.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(roleName));
            List<string> userIds = role.Users.Select(u => u.UserId).ToList();
            return context.Users.Where(u => userIds.Contains(u.Id) && u.Enabled && !u.DisableNotification).Select(u => u.Email).ToList();
        }

        private static void NotifyHomeowner(ApplicationDbContext context, Submission submission, IEmailSender mail, Stream file, string attachmentName)
        {
            var subject = String.Format("ARB Submission {0}", submission.Code);
            string status = GetStatusText(submission.Status);
            var link = String.Format("{0}/Submission/ViewStatus/{1}", BaseHost, submission.Code);
            string emailHtml = String.Format(m_homeownerEmail, submission.FirstName, submission.LastName, status, submission.Code, link);
            mail.SendEmailAsync(new List<string> { submission.Email }, string.Format("ARB: New submission {0}", submission.Code), emailHtml, file, attachmentName);
        }

        private static string GetStatusText(Status s)
        {
            string status;
            if (s == Status.Submitted)
            {
                status = "has been received";
            }
            else if (s == Status.Approved)
            {
                status = "has been approved";
            }
            else if (s == Status.ConditionallyApproved)
            {
                status = "has been conditionally approved";
            }
            else if (s == Status.MissingInformation)
            {
                status = "has been returned due to missing information";
            }
            else
            {
                status = "has been rejected";
            }

            return status;
        }

        public static List<string> GetOverdueRecipients(ApplicationDbContext context, Submission submission)
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
                var role = context.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name.Equals(RoleNames.ARBBoardMember));
                List<string> userIds = role.Users.Select(u => u.UserId).ToList();
                var board = context.Users.Where(u => userIds.Contains(u.Id) && u.Enabled && !u.DisableNotification).Select(u => u.Id).ToList();
                var alreadyReviewed = context.Reviews.Where(r => r.Submission.Id == submission.Id).Select(r => r.Reviewer.Id).ToList();
                var toReview = board.Except(alreadyReviewed);
                emails = context.Users.Where(u => toReview.Contains(u.Id) && u.Enabled && !u.DisableNotification).Select(u => u.Email).ToList();
            }
            else
            {
                emails = GetRoleMembers(context, roleToNofity);
            }

            return emails;
        }
        
        public static void NotifySubmissonsOverdue(string email, List<Submission> submissions, IEmailSender mail)
        {
            string body = "";

            foreach (var submission in submissions)
            {
                var link = String.Format("{0}/Submission/View/{1}", BaseHost, submission.Id);
                string html = string.Format("<a href='{0}'>{0}</a><br>", link);
                body = body + "\n" + html;
            }
                                   
            var emailHtml = String.Format(m_overdueEmail, body);
            mail.SendEmailAsync(new List<string> { email }, "ARB: Overdue submission", emailHtml, null, null);
        }
    }
}
