using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string recipient, string subject, string message, Stream attachment, string attachmentName);
    }
}
