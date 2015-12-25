using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using SendGrid;

namespace HOA.Services
{
    public class SendGridEmail : IEmailSender
    {
        public static string ApiUser;
        public static string ApiPass;
        public static string EmailSource;

        public Task SendEmailAsync(List<string> recipients, string subject, string message)
        {
            // Create the email object first, then add the properties.
            var myMessage = new SendGridMessage();

            // Add the message properties.
            myMessage.From = new MailAddress(EmailSource);
            
            myMessage.AddTo(recipients);

            myMessage.Subject = subject;

            //Add the HTML and Text bodies
            myMessage.Html = message;
            //myMessage.Text = "Hello World plain text!";

            var credentials = new System.Net.NetworkCredential(ApiUser, ApiPass);
            
            var transportWeb = new Web(credentials);
            
            // Send the email.
            return transportWeb.DeliverAsync(myMessage);
        }
    }
}
