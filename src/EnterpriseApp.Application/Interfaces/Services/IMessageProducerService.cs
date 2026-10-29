namespace EnterpriseApp.Application.Interfaces.Services;

public interface IMessageProducerService
{
    Task<string> PublishMessageAsync(
        string content,
        string subject,
        IDictionary<string, string>? properties = null,
        CancellationToken ct = default);
}
