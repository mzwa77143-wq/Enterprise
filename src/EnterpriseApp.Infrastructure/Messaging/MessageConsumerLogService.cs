using System.Collections.Concurrent;
using EnterpriseApp.Application.Interfaces.Services;
using EnterpriseApp.Domain.Entities;

namespace EnterpriseApp.Infrastructure.Messaging;

public class MessageConsumerLogService : IMessageConsumerLogService
{
    private readonly ConcurrentDictionary<string, ServiceBusMessageRecord> _messages = new();
    private int _totalEnqueued;
    private int _totalCompleted;
    private int _totalDeadLettered;
    private string _activeTransportMode = "Azure Service Bus";

    public void SetTransportMode(string mode) => _activeTransportMode = mode;

    public Task LogMessageReceivedAsync(ServiceBusMessageRecord message)
    {
        _messages[message.MessageId] = message;
        Interlocked.Increment(ref _totalEnqueued);
        return Task.CompletedTask;
    }

    public Task LogMessageCompletedAsync(string messageId, string notes)
    {
        if (_messages.TryGetValue(messageId, out var record))
        {
            record.MarkCompleted(notes);
            Interlocked.Increment(ref _totalCompleted);
        }
        return Task.CompletedTask;
    }

    public Task LogMessageDeadLetteredAsync(string messageId, string reason)
    {
        if (_messages.TryGetValue(messageId, out var record))
        {
            record.MarkDeadLettered(reason);
            Interlocked.Increment(ref _totalDeadLettered);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ServiceBusMessageDto>> GetLogsAsync()
    {
        var logs = _messages.Values
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(100)
            .Select(m => new ServiceBusMessageDto
            {
                MessageId = m.MessageId,
                Subject = m.Subject,
                Body = m.Body,
                Status = m.Status,
                DeliveryCount = m.DeliveryCount,
                EnqueuedAtUtc = m.CreatedAtUtc,
                ProcessedAtUtc = m.ProcessedAtUtc,
                ProcessingNotes = m.ProcessingNotes,
                ApplicationProperties = new Dictionary<string, string>(m.ApplicationProperties)
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<ServiceBusMessageDto>>(logs);
    }

    public QueueMetricsDto GetMetrics()
    {
        return new QueueMetricsDto
        {
            TotalEnqueued = _totalEnqueued,
            TotalCompleted = _totalCompleted,
            TotalDeadLettered = _totalDeadLettered,
            ActiveTransportMode = _activeTransportMode
        };
    }

    public void ClearLogs()
    {
        _messages.Clear();
        Interlocked.Exchange(ref _totalEnqueued, 0);
        Interlocked.Exchange(ref _totalCompleted, 0);
        Interlocked.Exchange(ref _totalDeadLettered, 0);
    }
}
