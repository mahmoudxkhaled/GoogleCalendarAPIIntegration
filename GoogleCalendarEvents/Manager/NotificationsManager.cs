using System.Net.Mail;
using System.Net;

namespace GoogleCalendarEvents.API.Helper
{
    public class NotificationsManager
    {
        // Im using send grids for sending emails and this is the method
        public static MailMessage CreateMessage(string mail, string eventSummary)
        {
            MailMessage message = new MailMessage();
            message.From = new MailAddress("mahmoudxkhaled@gmail.com");
            message.Subject = "New Calendar Event Created";
            message.To.Add(new MailAddress(mail));
            string bodyMessage = $"The {eventSummary} Event Created Now you can check your Google Calendar to see the details";
            message.Body = $"<html><body> {bodyMessage} </body></html>";
            message.IsBodyHtml = true;
            return message;

        }
        public static void SendEmail(string mail, string eventSummary)
        {

            MailMessage message = CreateMessage(mail, eventSummary);
            var client = new SmtpClient("smtp.sendgrid.net", 587);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential("apikey", "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");
            client.Send(message);
        }


    }
}
