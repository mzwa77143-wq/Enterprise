using EnterpriseApp.Domain.Common;

namespace EnterpriseApp.Domain.Entities;

public class ServiceBusMessageRecord : BaseEntity
{
    public string MessageId { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Enqueued";
    public int DeliveryCount { get; private set; } = 1;
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ProcessingNotes { get; private set; }
    public Dictionary<string, string> ApplicationProperties { get; private set; } = new();

    private ServiceBusMessageRecord() { }

    public ServiceBusMessageRecord(string messageId, string subject, string body, Dictionary<string, string>? properties = null)
    {
        MessageId = string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId;
        Subject = subject;
        Body = body;
        Status = "Enqueued";
        DeliveryCount = 1;
        ApplicationProperties = properties ?? new Dictionary<string, string>();
    }

    public void MarkCompleted(string notes = "Peek-lock completed successfully.")
    {
        Status = "Completed";
        ProcessedAtUtc = DateTime.UtcNow;
        ProcessingNotes = notes;
    }

    public void MarkDeadLettered(string reason)
    {
        Status = "DeadLettered";
        ProcessedAtUtc = DateTime.UtcNow;
        ProcessingNotes = $"Moved to Dead-Letter Queue. Reason: {reason}";
    }

    public void IncrementDeliveryCount()
    {
        DeliveryCount++;
    }
}
