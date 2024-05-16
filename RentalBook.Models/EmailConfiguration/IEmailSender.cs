namespace RentalBook.Models.EmailConfiguration
{
    public interface IEmailSender
    {
        void SendEmail(EmailMessage message);
    }
}
