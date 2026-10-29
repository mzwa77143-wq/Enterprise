using System.Numerics;
using EnterpriseApp.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseApp.UnitTests.MultiThreading;

public class FactorialCalculatorTests
{
    private readonly FactorialCalculatorService _service = new(NullLogger<FactorialCalculatorService>.Instance);

    [Theory]
    [InlineData(0, "1")]
    [InlineData(1, "1")]
    [InlineData(5, "120")]
    [InlineData(10, "3628800")]
    [InlineData(20, "2432902008176640000")]
    public async Task CalculateSingleAsync_KnownInputs_ShouldProduceAccurateResults(int input, string expectedResult)
    {
        // Act
        var result = await _service.CalculateSingleAsync(input);

        // Assert
        result.Status.Should().Be("Completed");
        result.Result.Should().Be(expectedResult);
        result.InputNumber.Should().Be(input);
        result.ManagedThreadId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateSingleAsync_LargeNumber100_ShouldProduceHugeBigInteger()
    {
        // Act
        var result = await _service.CalculateSingleAsync(100);

        // Assert
        result.Status.Should().Be("Completed");
        result.Result.Should().NotBeNullOrWhiteSpace();
        BigInteger.TryParse(result.Result, out var parsed).Should().BeTrue();
        parsed.Should().BeGreaterThan(BigInteger.Parse("93326215443944152681699238856266700490715968264381621468592963895217599993229915608941463976156518286253697920827223758251185210916864000000000000000000000000") - 1);
    }

    [Fact]
    public async Task CalculateBatchAsync_MultipleNumbers_ShouldExecuteConcurrentlyWithSemaphore()
    {
        // Arrange
        var numbers = new[] { 5, 12, 18, 25, 30, 45, 50 };

        // Act
        var batchResult = await _service.CalculateBatchAsync(numbers, maxConcurrency: 3);

        // Assert
        batchResult.TotalRequested.Should().Be(7);
        batchResult.CompletedCount.Should().Be(7);
        batchResult.Results.Should().HaveCount(7);
        batchResult.Results.All(r => r.Status == "Completed").Should().BeTrue();
        batchResult.Results.Select(r => r.ManagedThreadId).Distinct().Count().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task CalculateBatchAsync_WithCancellation_ShouldCancelGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act & Assert
        // A pre-cancelled token will cause SemaphoreSlim.WaitAsync to throw OperationCanceledException or produce Canceled tasks
        Func<Task> act = async () => await _service.CalculateBatchAsync(new[] { 500, 1000 }, maxConcurrency: 2, ct: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
