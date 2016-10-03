using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class MockEmail : IEmailSender
    {
        private ILogger<MockEmail> _logger;

        public MockEmail(ILogger<MockEmail> logger)
        {
            _logger = logger;
        }

        private void SendEmail(string recipient, string subject, string message)
        {
            _logger.LogWarning("Sending email to {0}", recipient);
        }

        public Task SendEmailAsync(string recipient, string subject, string message, Stream attachment, string attachmentName)
        {
            return Task.Factory.StartNew(() => SendEmail(recipient, subject, message));
        }
    }
}
