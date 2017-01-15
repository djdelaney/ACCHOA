using HOA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Tests.Helpers
{
    public class TestEmail : IEmailSender
    {
        public class Email
        {
            public string Recipient { get; set; }
            public string Subject { get; set; }
            public string Message { get; set; }
        }

        public List<Email> Emails = new List<Email>();

        public Task SendEmailAsync(string recipient, string subject, string message, Stream attachment, string attachmentName)
        {
            Emails.Add(new Email
            {
                Message = message,
                Subject = subject,
                Recipient = recipient
            });

            return Task.Factory.StartNew(() => Console.WriteLine("EMAIL!"));
        }
    }
}
