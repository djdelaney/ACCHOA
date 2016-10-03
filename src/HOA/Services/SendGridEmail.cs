using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class SendGridEmail : IEmailSender
    {
        public static string ApiKey;
        public static string EmailSource;

        public Task SendEmailAsync(string recipient, string subject, string message, Stream attachmentStream, string attachmentName)
        {
            dynamic sg = new SendGrid.SendGridAPIClient(ApiKey, "https://api.sendgrid.com");

            Email from = new Email(EmailSource, "Applecross ARB");
            Email to = new Email(recipient);
            Content content = new Content("text/html", message);
            Mail mail = new Mail(from, subject, to, content);
            
            if (attachmentStream != null && !string.IsNullOrEmpty(attachmentName))
            {
                byte[] bytes = ReadFully(attachmentStream);
                Attachment attachment = new Attachment();
                attachment.Content = Convert.ToBase64String(bytes);
                attachment.Type = "application/pdf";
                attachment.Filename = attachmentName;
                attachment.Disposition = "attachment";
                mail.AddAttachment(attachment);
            }

            return Task.Run(async () =>
            {
                dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
            });
        }

        private static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
