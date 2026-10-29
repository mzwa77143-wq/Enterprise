using EnterpriseApp.Domain.Entities;

namespace EnterpriseApp.Application.Interfaces.Services;

public class QueueMetricsDto
{
    public int TotalEnqueued { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalDeadLettered { get; set; }
    public int PendingCount => Math.Max(0, TotalEnqueued - (TotalCompleted + TotalDeadLettered));
    public string ActiveTransportMode { get; set; } = "Azure Service Bus (Resilient Emulator / Live)";
}

public class ServiceBusMessageDto
{
    public string MessageId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int DeliveryCount { get; set; }
    public DateTime EnqueuedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ProcessingNotes { get; set; }
    public Dictionary<string, string> ApplicationProperties { get; set; } = new();
}

public interface IMessageConsumerLogService
{
    Task LogMessageReceivedAsync(ServiceBusMessageRecord message);
    Task LogMessageCompletedAsync(string messageId, string notes);
    Task LogMessageDeadLetteredAsync(string messageId, string reason);
    Task<IReadOnlyList<ServiceBusMessageDto>> GetLogsAsync();
    QueueMetricsDto GetMetrics();
    void ClearLogs();
}
