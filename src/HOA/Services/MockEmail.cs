using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private void SendEmail(List<string> emails, string subject, string message)
        {
            _logger.LogWarning("Sending email to {0}", string.Join(", ", emails));
        }

        public Task SendEmailAsync(List<string> emails, string subject, string message)
        {
            return Task.Factory.StartNew(() => SendEmail(emails, subject, message));
        }
    }
}
