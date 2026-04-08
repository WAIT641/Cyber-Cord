using System.Net;
using System.Net.Mail;
using Cyber_Cord.Api.Exceptions;
using Cyber_Cord.Api.Options;
using Cyber_Cord.Api.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Cyber_Cord.Api.Services;

public class EmailSender(
    IOptions<EmailSenderOptions> optionsAccessor,
    IStringLocalizer<EmailMessages> localizer,
    IConfiguration configuration
) : ICustomEmailSender, IEmailSender {
    private const string _validationCodeEmail = "ValidationCodeEmail";
    private const string _noticeEmail = "ActivateAccountNoticeEmail";

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var key = optionsAccessor.Value.EmailAuthKey;
        var sourceEmail = optionsAccessor.Value.SourceEmailAddress;
        var username = optionsAccessor.Value.EmailUsername;
        var host = optionsAccessor.Value.EmailHost;

        if (key is null)
        {
            throw new EmailSenderException("The `EmailAuthKey` secret was not set, so emails cannot be sent from this application");
        }

        var mailAddress = new MailAddress(sourceEmail, username);

        using var smtpClient = new SmtpClient
        {
            Host = host,
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(sourceEmail, key),
        };


        using var mailMessage = new MailMessage(sourceEmail, email)
        {
            Subject = subject,
            Body = message,
            Sender = mailAddress,
            IsBodyHtml = true
        };

        await smtpClient.SendMailAsync(mailMessage);
    }

    public string GetValidationEmailMessage(string validationToken, int userId)
    {
        var message = localizer[_validationCodeEmail];

        return string.Format(message, validationToken, configuration[Constants.ConfigurationConstants.ClientAddress], userId);
    }
    
    public string GetNoticeEmailMessage(string accountName)
    {
        var message = localizer[_noticeEmail];

        return string.Format(message, accountName);
    }
}
