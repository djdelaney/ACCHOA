using Microsoft.Framework.Logging;
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

        private void SendEmail(string email, string subject, string message)
        {
            _logger.LogWarning("Sending email to {0}", email);
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.Factory.StartNew(() => SendEmail(email, subject, message));
        }
    }
}
