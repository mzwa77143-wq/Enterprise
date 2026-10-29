namespace EnterpriseApp.Application.DTOs.Factorial;

public class BatchCalculationRequest
{
    public List<int> Numbers { get; set; } = new();
    public int MaxConcurrency { get; set; } = 4;
}
