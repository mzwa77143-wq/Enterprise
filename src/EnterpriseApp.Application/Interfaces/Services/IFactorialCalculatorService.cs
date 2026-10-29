using EnterpriseApp.Domain.Entities;

namespace EnterpriseApp.Application.Interfaces.Services;

public class BatchCalculationResultDto
{
    public Guid BatchId { get; set; } = Guid.NewGuid();
    public int TotalRequested { get; set; }
    public int CompletedCount { get; set; }
    public long TotalElapsedMilliseconds { get; set; }
    public List<FactorialTaskDto> Results { get; set; } = new();
}

public class FactorialTaskDto
{
    public Guid Id { get; set; }
    public int InputNumber { get; set; }
    public string? Result { get; set; }
    public string Status { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
    public int ManagedThreadId { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IFactorialCalculatorService
{
    Task<BatchCalculationResultDto> CalculateBatchAsync(
        IEnumerable<int> numbers,
        int maxConcurrency = 4,
        IProgress<FactorialTaskDto>? progress = null,
        CancellationToken ct = default);

    Task<FactorialTaskDto> CalculateSingleAsync(int number, CancellationToken ct = default);
}
