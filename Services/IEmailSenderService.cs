using System.Threading.Tasks;

namespace TripAnalyzer.Services
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendAdminNotificationAsync(string userName, string userEmail, string subject, string message);
    }
}
