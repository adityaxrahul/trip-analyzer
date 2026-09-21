using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TripAnalyzer.Services
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(
            IConfiguration config,
            ILogger<EmailSenderService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // General email
        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body)
        {
            var adminEmail = GetConfig("ADMIN_EMAIL");

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "ADMIN_EMAIL environment variable is not configured.");
            }

            await SendEmailInternalAsync(
                adminEmail,
                subject,
                body,
                null,
                true);
        }

        // Contact form -> Admin email
        public async Task SendAdminNotificationAsync(
            string userName,
            string userEmail,
            string subject,
            string message)
        {
            var adminEmail = GetConfig("ADMIN_EMAIL");

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "ADMIN_EMAIL environment variable is not configured.");
            }

            var submittedAt =
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

            var body =
$"""
Trip Analyzer
New Contact Enquiry

Name: {userName}
Email: {userEmail}
Subject: {subject}

Message:
{message}

Submitted At:
{submittedAt}
""";

            await SendEmailInternalAsync(
                adminEmail,
                $"Trip Analyzer - New Contact Enquiry: {subject}",
                body,
                userEmail,
                false);
        }

        // Central SMTP sender
        private async Task SendEmailInternalAsync(
            string toEmail,
            string subject,
            string body,
            string? replyToEmail,
            bool isHtml)
        {
            var host = GetConfig("SMTP_HOST");
            var portString = GetConfig("SMTP_PORT");
            var username = GetConfig("SMTP_USERNAME");
            var password = GetConfig("SMTP_PASSWORD");

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                _logger.LogError(
                    "SMTP configuration incomplete. Host={Host}, Port={Port}, UsernameConfigured={UsernameConfigured}, PasswordConfigured={PasswordConfigured}",
                    host ?? "NULL",
                    portString ?? "NULL",
                    !string.IsNullOrWhiteSpace(username),
                    !string.IsNullOrWhiteSpace(password));

                throw new InvalidOperationException(
                    "SMTP configuration is incomplete.");
            }

            if (!int.TryParse(portString, out int port))
            {
                port = 587;
            }

            try
            {
                _logger.LogInformation(
                    "Starting SMTP delivery using {Host}:{Port}",
                    host,
                    port);

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),

                    // Gmail SMTP submission uses STARTTLS on port 587.
                    EnableSsl = true,

                    UseDefaultCredentials = false,

                    DeliveryMethod = SmtpDeliveryMethod.Network,

                    // Prevent very long hanging requests.
                    Timeout = 30000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(
                        username,
                        "Trip Analyzer"),

                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(
                    new MailAddress(toEmail));

                if (!string.IsNullOrWhiteSpace(replyToEmail))
                {
                    try
                    {
                        mailMessage.ReplyToList.Add(
                            new MailAddress(replyToEmail));
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning(
                            "Invalid Reply-To address: {ReplyTo}",
                            replyToEmail);
                    }
                }

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation(
                    "SMTP email successfully sent to {Recipient}",
                    toEmail);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP delivery failed. Host={Host}, Port={Port}, Recipient={Recipient}",
                    host,
                    port,
                    toEmail);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected email error. Host={Host}, Port={Port}",
                    host,
                    port);

                throw;
            }
        }

        private string? GetConfig(string key)
        {
            return _config[key]
                   ?? Environment.GetEnvironmentVariable(key);
        }
    }
}