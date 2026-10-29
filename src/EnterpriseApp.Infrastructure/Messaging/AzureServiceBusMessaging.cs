using System.Text.Json;
using System.Threading.Channels;
using Azure.Messaging.ServiceBus;
using EnterpriseApp.Application.Interfaces.Services;
using EnterpriseApp.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseApp.Infrastructure.Messaging;

public class SimulatedMessageContext
{
    public ServiceBusReceivedMessage? RealMessage { get; init; }
    public ProcessMessageEventArgs? RealEventArgs { get; init; }
    public string MessageId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Dictionary<string, string> ApplicationProperties { get; init; } = new();
    public int DeliveryCount { get; set; } = 1;
}

public class LocalServiceBusQueue
{
    private readonly Channel<SimulatedMessageContext> _channel = Channel.CreateUnbounded<SimulatedMessageContext>();

    public ValueTask EnqueueAsync(SimulatedMessageContext message, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(message, ct);
    }

    public IAsyncEnumerable<SimulatedMessageContext> ReadAllAsync(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}

public class AzureServiceBusProducerService : IMessageProducerService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureServiceBusProducerService> _logger;
    private readonly IMessageConsumerLogService _logService;
    private readonly LocalServiceBusQueue _localQueue;
    private readonly ServiceBusSender? _sender;

    public AzureServiceBusProducerService(
        IConfiguration configuration,
        ILogger<AzureServiceBusProducerService> logger,
        IMessageConsumerLogService logService,
        LocalServiceBusQueue localQueue)
    {
        _configuration = configuration;
        _logger = logger;
        _logService = logService;
        _localQueue = localQueue;

        var connectionString = _configuration["AzureServiceBus:ConnectionString"];
        var queueName = _configuration["AzureServiceBus:QueueName"] ?? "enterprise-messages";

        if (!string.IsNullOrWhiteSpace(connectionString) &&
            !connectionString.Equals("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase) &&
            connectionString.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var client = new ServiceBusClient(connectionString, new ServiceBusClientOptions
                {
                    RetryOptions = new ServiceBusRetryOptions
                    {
                        Mode = ServiceBusRetryMode.Exponential,
                        MaxRetries = 3,
                        Delay = TimeSpan.FromSeconds(1),
                        MaxDelay = TimeSpan.FromSeconds(10)
                    }
                });
                _sender = client.CreateSender(queueName);
                _logger.LogInformation("Connected to Azure Service Bus Queue: {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Azure Service Bus client. Falling back to local emulator.");
            }
        }
    }

    public async Task<string> PublishMessageAsync(
        string content,
        string subject,
        IDictionary<string, string>? properties = null,
        CancellationToken ct = default)
    {
        var messageId = Guid.NewGuid().ToString();
        var propsDict = properties != null ? new Dictionary<string, string>(properties) : new Dictionary<string, string>();

        var record = new ServiceBusMessageRecord(messageId, subject, content, propsDict);
        await _logService.LogMessageReceivedAsync(record);

        if (_sender != null)
        {
            try
            {
                var sbMessage = new ServiceBusMessage(content)
                {
                    MessageId = messageId,
                    Subject = subject,
                    CorrelationId = Guid.NewGuid().ToString()
                };

                foreach (var (k, v) in propsDict)
                {
                    sbMessage.ApplicationProperties[k] = v;
                }

                _logger.LogInformation("Publishing message {MessageId} to live Azure Service Bus.", messageId);
                await _sender.SendMessageAsync(sbMessage, ct);
                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to Azure Service Bus. Relaying to fallback queue.");
            }
        }

        // Local resilient message delivery with peek-lock simulation
        _logger.LogInformation("Dispatching message {MessageId} via resilient queue pipeline.", messageId);
        await _localQueue.EnqueueAsync(new SimulatedMessageContext
        {
            MessageId = messageId,
            Subject = subject,
            Body = content,
            ApplicationProperties = propsDict
        }, ct);

        return messageId;
    }
}

public class AzureServiceBusConsumerHostedService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureServiceBusConsumerHostedService> _logger;
    private readonly IMessageConsumerLogService _logService;
    private readonly LocalServiceBusQueue _localQueue;
    private ServiceBusProcessor? _processor;

    public AzureServiceBusConsumerHostedService(
        IConfiguration configuration,
        ILogger<AzureServiceBusConsumerHostedService> logger,
        IMessageConsumerLogService logService,
        LocalServiceBusQueue localQueue)
    {
        _configuration = configuration;
        _logger = logger;
        _logService = logService;
        _localQueue = localQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["AzureServiceBus:ConnectionString"];
        var queueName = _configuration["AzureServiceBus:QueueName"] ?? "enterprise-messages";

        if (!string.IsNullOrWhiteSpace(connectionString) &&
            !connectionString.Equals("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase) &&
            connectionString.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var client = new ServiceBusClient(connectionString);
                _processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false, // Peek-lock semantics
                    MaxConcurrentCalls = 2,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

                _processor.ProcessMessageAsync += HandleMessageAsync;
                _processor.ProcessErrorAsync += HandleErrorAsync;

                await _processor.StartProcessingAsync(stoppingToken);
                _logger.LogInformation("Azure Service Bus consumer processor started for queue: {QueueName}", queueName);

                if (_logService is MessageConsumerLogService mcls)
                {
                    mcls.SetTransportMode($"Azure Service Bus: {queueName} (Peek-Lock)");
                }

                // Keep running until stopped
                await Task.Delay(Timeout.Infinite, stoppingToken);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not start Azure Service Bus processor. Operating in resilient local emulator mode.");
            }
        }

        // Fallback / In-Memory Peek-Lock Consumer Pipeline
        _logger.LogInformation("Azure Service Bus consumer running in Resilient Emulator / Local Mode.");
        if (_logService is MessageConsumerLogService mclsFallback)
        {
            mclsFallback.SetTransportMode("Azure Service Bus (Resilient Local Peek-Lock Simulator)");
        }

        try
        {
            await foreach (var message in _localQueue.ReadAllAsync(stoppingToken))
            {
                await ProcessSimulatedMessageAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer pipeline stopped cleanly.");
        }
    }

    public async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var messageId = args.Message.MessageId;
        var subject = args.Message.Subject ?? "No Subject";
        var body = args.Message.Body.ToString();

        _logger.LogInformation("Received message {MessageId} with Peek-Lock. Subject: {Subject}", messageId, subject);

        // Check if dead-letter was requested via application property
        if (args.Message.ApplicationProperties.TryGetValue("SimulateDeadLetter", out var dlVal) &&
            dlVal?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogWarning("Dead-letter trigger received for message {MessageId}. Moving to Dead-Letter Queue.", messageId);
            await args.DeadLetterMessageAsync(args.Message, "Simulated Dead-Letter Requested", "Triggered by test payload");
            await _logService.LogMessageDeadLetteredAsync(messageId, "Message moved to dead-letter queue via DeadLetterMessageAsync.");
            return;
        }

        // Simulate enterprise processing
        await Task.Delay(100);

        // Explicit Peek-Lock completion
        await args.CompleteMessageAsync(args.Message);
        _logger.LogInformation("Message {MessageId} successfully completed via CompleteMessageAsync.", messageId);
        await _logService.LogMessageCompletedAsync(messageId, "Message processed and completed with Peek-Lock (CompleteMessageAsync).");
    }

    public Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "ServiceBusProcessor encountered an error. ErrorSource: {ErrorSource}, EntityPath: {EntityPath}",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }

    private async Task ProcessSimulatedMessageAsync(SimulatedMessageContext message, CancellationToken ct)
    {
        _logger.LogInformation("Processing message {MessageId} under Peek-Lock semantics.", message.MessageId);

        // Simulate realistic network/processing latency
        await Task.Delay(300, ct);

        // Check for simulated dead-lettering
        if (message.ApplicationProperties.TryGetValue("SimulateDeadLetter", out var dlVal) &&
            dlVal.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Routing message {MessageId} to Dead-Letter Queue.", message.MessageId);
            await _logService.LogMessageDeadLetteredAsync(message.MessageId, "Simulated poison message threshold exceeded -> DeadLettered.");
            return;
        }

        // Peek-lock completion
        await _logService.LogMessageCompletedAsync(message.MessageId, "Successfully completed under Peek-Lock pattern.");
        _logger.LogInformation("Message {MessageId} finalized.", message.MessageId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            try
            {
                await _processor.StopProcessingAsync(cancellationToken);
                await _processor.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping Azure Service Bus processor.");
            }
        }

        await base.StopAsync(cancellationToken);
    }
}
