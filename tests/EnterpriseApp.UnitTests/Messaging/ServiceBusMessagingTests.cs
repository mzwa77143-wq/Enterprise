using EnterpriseApp.Application.Interfaces.Services;
using EnterpriseApp.Domain.Entities;
using EnterpriseApp.Infrastructure.Messaging;
using EnterpriseApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EnterpriseApp.UnitTests.Messaging;

public class ServiceBusMessagingTests
{
    [Fact]
    public async Task ProducerService_PublishMessageAsync_InLocalMode_ShouldLogAndEnqueueMessage()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AzureServiceBus:ConnectionString"] = "UseDevelopmentEmulator=true",
            ["AzureServiceBus:QueueName"] = "test-queue"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var mockLogService = new Mock<IMessageConsumerLogService>();
        var localQueue = new LocalServiceBusQueue();
        var producer = new AzureServiceBusProducerService(
            config,
            NullLogger<AzureServiceBusProducerService>.Instance,
            mockLogService.Object,
            localQueue);

        // Act
        var messageId = await producer.PublishMessageAsync("Payload data", "OrderCreated");

        // Assert
        messageId.Should().NotBeNullOrWhiteSpace();
        mockLogService.Verify(l => l.LogMessageReceivedAsync(It.Is<ServiceBusMessageRecord>(r =>
            r.MessageId == messageId && r.Subject == "OrderCreated" && r.Body == "Payload data")), Times.Once);
    }

    [Fact]
    public async Task ConsumerHostedService_ExecuteAsync_ShouldProcessMessageAndCompleteWithPeekLock()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AzureServiceBus:ConnectionString"] = "UseDevelopmentEmulator=true"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var logService = new MessageConsumerLogService();
        var localQueue = new LocalServiceBusQueue();

        var consumer = new AzureServiceBusConsumerHostedService(
            config,
            NullLogger<AzureServiceBusConsumerHostedService>.Instance,
            logService,
            localQueue);

        using var cts = new CancellationTokenSource();

        // Start background consumer
        var consumerTask = consumer.StartAsync(cts.Token);

        // Act: Enqueue a test message
        var messageId = Guid.NewGuid().ToString();
        await logService.LogMessageReceivedAsync(new ServiceBusMessageRecord(messageId, "InventoryUpdated", "Item 42 stock: 100"));
        await localQueue.EnqueueAsync(new SimulatedMessageContext
        {
            MessageId = messageId,
            Subject = "InventoryUpdated",
            Body = "Item 42 stock: 100"
        });

        // Wait briefly for the simulated consumer loop to process
        await Task.Delay(600);

        // Assert
        var messageLogs = await logService.GetLogsAsync();
        var completedLog = messageLogs.FirstOrDefault(m => m.MessageId == messageId && m.Status == "Completed");
        completedLog.Should().NotBeNull();
        completedLog!.Status.Should().Be("Completed");

        // Clean up
        cts.Cancel();
        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ConsumerHostedService_DeadLetterTrigger_ShouldRouteToDeadLetterQueue()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["AzureServiceBus:ConnectionString"] = "UseDevelopmentEmulator=true"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var logService = new MessageConsumerLogService();
        var localQueue = new LocalServiceBusQueue();

        var consumer = new AzureServiceBusConsumerHostedService(
            config,
            NullLogger<AzureServiceBusConsumerHostedService>.Instance,
            logService,
            localQueue);

        using var cts = new CancellationTokenSource();
        await consumer.StartAsync(cts.Token);

        // Act: Enqueue a poison message with SimulateDeadLetter flag
        var messageId = Guid.NewGuid().ToString();
        await logService.LogMessageReceivedAsync(new ServiceBusMessageRecord(messageId, "CorruptPayload", "{ corrupt json }"));
        await localQueue.EnqueueAsync(new SimulatedMessageContext
        {
            MessageId = messageId,
            Subject = "CorruptPayload",
            Body = "{ corrupt json }",
            ApplicationProperties = new Dictionary<string, string> { ["SimulateDeadLetter"] = "true" }
        });

        // Wait for processing
        await Task.Delay(600);

        // Assert
        var messageLogs = await logService.GetLogsAsync();
        var deadLettered = messageLogs.FirstOrDefault(m => m.MessageId == messageId && m.Status == "DeadLettered");
        deadLettered.Should().NotBeNull();
        deadLettered!.Status.Should().Be("DeadLettered");

        // Clean up
        cts.Cancel();
        await consumer.StopAsync(CancellationToken.None);
    }
}
