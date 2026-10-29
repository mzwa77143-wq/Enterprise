using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using EnterpriseApp.Application.Interfaces.Services;
using EnterpriseApp.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EnterpriseApp.Infrastructure.Services;

public class FactorialCalculatorService : IFactorialCalculatorService
{
    private readonly ILogger<FactorialCalculatorService> _logger;

    public FactorialCalculatorService(ILogger<FactorialCalculatorService> logger)
    {
        _logger = logger;
    }

    public async Task<BatchCalculationResultDto> CalculateBatchAsync(
        IEnumerable<int> numbers,
        int maxConcurrency = 4,
        IProgress<FactorialTaskDto>? progress = null,
        CancellationToken ct = default)
    {
        var numberList = numbers.Distinct().ToList();
        var totalStopwatch = Stopwatch.StartNew();

        int effectiveConcurrency = Math.Clamp(maxConcurrency, 1, 32);
        using var semaphore = new SemaphoreSlim(effectiveConcurrency, effectiveConcurrency);
        var resultsBag = new ConcurrentBag<FactorialTaskDto>();

        _logger.LogInformation("Starting batch factorial calculation for {Count} numbers with concurrency level {Concurrency}.",
            numberList.Count, effectiveConcurrency);

        var tasks = numberList.Select(async number =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var taskResult = await CalculateSingleInternalAsync(number, ct);
                resultsBag.Add(taskResult);
                progress?.Report(taskResult);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        totalStopwatch.Stop();

        var sortedResults = resultsBag.OrderBy(r => r.InputNumber).ToList();

        return new BatchCalculationResultDto
        {
            BatchId = Guid.NewGuid(),
            TotalRequested = numberList.Count,
            CompletedCount = sortedResults.Count(r => r.Status == "Completed"),
            TotalElapsedMilliseconds = totalStopwatch.ElapsedMilliseconds,
            Results = sortedResults
        };
    }

    public async Task<FactorialTaskDto> CalculateSingleAsync(int number, CancellationToken ct = default)
    {
        return await CalculateSingleInternalAsync(number, ct);
    }

    private static async Task<FactorialTaskDto> CalculateSingleInternalAsync(int number, CancellationToken ct)
    {
        var domainTask = new FactorialTask(number);
        int threadId = Environment.CurrentManagedThreadId;
        domainTask.MarkProcessing(threadId);

        var sw = Stopwatch.StartNew();
        try
        {
            // Execute on thread pool to demonstrate true asynchronous multi-threading
            var factorialBigInt = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                return ComputeFactorial(number, ct);
            }, ct);

            sw.Stop();
            domainTask.Complete(factorialBigInt.ToString(), sw.ElapsedMilliseconds, Environment.CurrentManagedThreadId);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            domainTask.Cancel();
        }
        catch (Exception ex)
        {
            sw.Stop();
            domainTask.Fail(ex.Message, sw.ElapsedMilliseconds, Environment.CurrentManagedThreadId);
        }

        return new FactorialTaskDto
        {
            Id = domainTask.Id,
            InputNumber = domainTask.InputNumber,
            Result = domainTask.Result,
            Status = domainTask.Status.ToString(),
            ElapsedMilliseconds = domainTask.ElapsedMilliseconds,
            ManagedThreadId = domainTask.ManagedThreadId,
            ErrorMessage = domainTask.ErrorMessage
        };
    }

    private static BigInteger ComputeFactorial(int n, CancellationToken ct)
    {
        if (n == 0 || n == 1) return BigInteger.One;

        BigInteger result = BigInteger.One;
        for (int i = 2; i <= n; i++)
        {
            if ((i & 0xFF) == 0) ct.ThrowIfCancellationRequested();
            result *= i;
        }
        return result;
    }
}
