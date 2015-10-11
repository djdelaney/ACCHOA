using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class SendGridEmail : IEmailSender
    {
        public Task SendEmailAsync(List<string> email, string subject, string message)
        {
            throw new NotImplementedException();
        }
    }
}
