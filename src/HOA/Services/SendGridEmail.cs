using SendGrid;
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
            var client = new SendGridClient(ApiKey);

            var from = new EmailAddress(EmailSource, "Applecross ARB");
            var to = new EmailAddress(recipient);
            
            SendGridMessage mail = MailHelper.CreateSingleEmail(from, to, subject, null, message);

            if (attachmentStream != null && !string.IsNullOrEmpty(attachmentName))
            {
                byte[] bytes = ReadFully(attachmentStream);
                Attachment attachment = new Attachment();
                attachment.Content = Convert.ToBase64String(bytes);
                attachment.Type = "application/pdf";
                attachment.Filename = attachmentName;
                attachment.Disposition = "attachment";

                mail.Attachments = new List<Attachment>();
                mail.Attachments.Add(attachment);
            }

            return Task.Run(async () =>
            {
                var response = await client.SendEmailAsync(mail);
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
