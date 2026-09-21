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
            // Fail loudly if ADMIN_EMAIL is not configured (S-01 fix)
            var adminEmail = _config["ADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "ADMIN_EMAIL environment variable is not set. Email routing cannot proceed without a configured admin address.");
            }
            await SendEmailInternalAsync(
                toEmail,
                subject,
                body,
                adminEmail,
                isHtml: true);
        }

        // Contact form -> Admin email
        public async Task SendAdminNotificationAsync(
            string userName,
            string userEmail,
            string subject,
            string message)
        {
            // Fail loudly if ADMIN_EMAIL is not configured (S-01 fix)
            var adminEmail = _config["ADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException(
                    "ADMIN_EMAIL environment variable is not set. Cannot dispatch admin notification.");
            }

            var submittedAt =
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";

            var body =
$@"Trip Analyzer
New Contact Enquiry

Name: {userName}
Email: {userEmail}
Subject: {subject}

Message:
{message}

Submitted At:
{submittedAt}";

            await SendEmailInternalAsync(
                adminEmail,
                $"Trip Analyzer - New Contact Enquiry: {subject}",
                body,
                userEmail,
                isHtml: false);
        }

        // Central SMTP email sender
        private async Task SendEmailInternalAsync(
            string toEmail,
            string subject,
            string body,
            string? replyToEmail,
            bool isHtml = false)
        {
            var host = _config["SMTP_HOST"] ?? Environment.GetEnvironmentVariable("SMTP_HOST");
            var portString = _config["SMTP_PORT"] ?? Environment.GetEnvironmentVariable("SMTP_PORT");
            var username = _config["SMTP_USERNAME"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = _config["SMTP_PASSWORD"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD");

            // SMTP configuration check
            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                _logger.LogError(
                    "SMTP configuration is incomplete. Host: '{Host}', Port: '{Port}'. HasUsername: {HasUser}, Password provided: {HasPassword}",
                    host ?? "NULL",
                    portString ?? "NULL",
                    !string.IsNullOrWhiteSpace(username),
                    !string.IsNullOrWhiteSpace(password));

                throw new InvalidOperationException("SMTP configuration is incomplete. Please set SMTP_HOST, SMTP_PORT, SMTP_USERNAME, and SMTP_PASSWORD environment variables.");
            }

            // Default SMTP port
            int port = 587;
            if (!string.IsNullOrWhiteSpace(portString) &&
                int.TryParse(portString, out var configuredPort))
            {
                port = configuredPort;
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(username, "Trip Analyzer"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(toEmail);

                if (!string.IsNullOrWhiteSpace(replyToEmail))
                {
                    try
                    {
                        mailMessage.ReplyToList.Add(new MailAddress(replyToEmail));
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning("Invalid Reply-To email address supplied: {ReplyTo}", replyToEmail);
                    }
                }

                // Downgrade SMTP details to Debug to avoid exposing details in production logs (S-16)
                _logger.LogDebug("Attempting SMTP email delivery. Host: {Host}, Port: {Port}, Recipient: {Recipient}", host, port, toEmail);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation("Email successfully sent to {ToEmail} via {Host}:{Port}.", toEmail, host, port);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP error while sending email. Host: {Host}, Port: {Port}, Recipient: {Recipient}, Error: {Message}",
                    host,
                    port,
                    toEmail,
                    ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while sending email. Host: {Host}, Port: {Port}, Recipient: {Recipient}, Error: {Message}",
                    host,
                    port,
                    toEmail,
                    ex.Message);
                throw;
            }
        }
    }
}