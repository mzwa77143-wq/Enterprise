using EnterpriseApp.Application.DTOs.Factorial;
using FluentValidation;

namespace EnterpriseApp.Application.Validators.Factorial;

public class BatchCalculationRequestValidator : AbstractValidator<BatchCalculationRequest>
{
    public BatchCalculationRequestValidator()
    {
        RuleFor(x => x.Numbers)
            .NotEmpty().WithMessage("At least one number must be supplied.")
            .Must(list => list.Count <= 100).WithMessage("Batch size cannot exceed 100 items.")
            .Must(list => list.All(n => n >= 0 && n <= 1000))
            .WithMessage("All input numbers must be non-negative integers between 0 and 1,000.");

        RuleFor(x => x.MaxConcurrency)
            .InclusiveBetween(1, 32)
            .WithMessage("MaxConcurrency must be between 1 and 32 threads.");
    }
}
