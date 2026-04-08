namespace Cyber_Cord.Api.Services;

public interface ICustomEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
    public string GetValidationEmailMessage(string validationToken, int userId);
    public string GetNoticeEmailMessage(string accountName);
}
