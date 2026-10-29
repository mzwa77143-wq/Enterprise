namespace EnterpriseApp.Application.Interfaces.Services;

public interface IMessageProducerService
{
    Task<string> PublishMessageAsync(
        string content,
        string subject,
        IDictionary<string, string>? properties = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GenerateAndEnqueueRandomNumbersAsync(
        int count = 5,
        int min = 1,
        int max = 1000,
        CancellationToken ct = default);
}
