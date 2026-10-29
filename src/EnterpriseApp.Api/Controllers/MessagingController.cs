using EnterpriseApp.Application.DTOs.Messaging;
using EnterpriseApp.Application.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagingController : ControllerBase
{
    private readonly IMessageProducerService _producerService;
    private readonly IMessageConsumerLogService _consumerLogService;
    private readonly IValidator<SendMessageRequest> _validator;

    public MessagingController(
        IMessageProducerService producerService,
        IMessageConsumerLogService consumerLogService,
        IValidator<SendMessageRequest> validator)
    {
        _producerService = producerService;
        _consumerLogService = consumerLogService;
        _validator = validator;
    }

    [HttpPost("send")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var props = request.Properties ?? new Dictionary<string, string>();
        if (request.SimulateDeadLetter)
        {
            props["SimulateDeadLetter"] = "true";
        }

        var messageId = await _producerService.PublishMessageAsync(request.Body, request.Subject, props, ct);

        return Accepted(new
        {
            MessageId = messageId,
            Status = "Enqueued",
            DispatchedAtUtc = DateTime.UtcNow
        });
    }

    [HttpPost("generate-random")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GenerateRandomNumbers(
        [FromQuery] int count = 5,
        [FromQuery] int min = 1,
        [FromQuery] int max = 1000,
        CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 50);
        if (min > max) (min, max) = (max, min);

        var messageIds = await _producerService.GenerateAndEnqueueRandomNumbersAsync(count, min, max, ct);

        return Accepted(new
        {
            GeneratedCount = messageIds.Count,
            MessageIds = messageIds,
            Status = "Enqueued",
            DispatchedAtUtc = DateTime.UtcNow
        });
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceBusMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs()
    {
        var logs = await _consumerLogService.GetLogsAsync();
        return Ok(logs);
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(QueueMetricsDto), StatusCodes.Status200OK)]
    public IActionResult GetMetrics()
    {
        var metrics = _consumerLogService.GetMetrics();
        return Ok(metrics);
    }

    [HttpPost("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ClearLogs()
    {
        _consumerLogService.ClearLogs();
        return NoContent();
    }
}
