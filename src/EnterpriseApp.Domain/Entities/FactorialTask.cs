using EnterpriseApp.Domain.Common;
using EnterpriseApp.Domain.Enums;

namespace EnterpriseApp.Domain.Entities;

public class FactorialTask : BaseEntity
{
    public int InputNumber { get; private set; }
    public string? Result { get; private set; }
    public CalculationStatus Status { get; private set; }
    public long ElapsedMilliseconds { get; private set; }
    public int ManagedThreadId { get; private set; }
    public string? ErrorMessage { get; private set; }

    public FactorialTask(int inputNumber)
    {
        if (inputNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(inputNumber), "Factorial is not defined for negative integers.");

        InputNumber = inputNumber;
        Status = CalculationStatus.Pending;
    }

    public void MarkProcessing(int threadId)
    {
        Status = CalculationStatus.Processing;
        ManagedThreadId = threadId;
    }

    public void Complete(string result, long elapsedMilliseconds, int threadId)
    {
        Result = result;
        ElapsedMilliseconds = elapsedMilliseconds;
        ManagedThreadId = threadId;
        Status = CalculationStatus.Completed;
    }

    public void Fail(string errorMessage, long elapsedMilliseconds, int threadId)
    {
        ErrorMessage = errorMessage;
        ElapsedMilliseconds = elapsedMilliseconds;
        ManagedThreadId = threadId;
        Status = CalculationStatus.Failed;
    }

    public void Cancel()
    {
        Status = CalculationStatus.Cancelled;
    }
}
