using EnterpriseApp.Application.DTOs.Messaging;
using FluentValidation;

namespace EnterpriseApp.Application.Validators.Messaging;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Message subject is required.")
            .MaximumLength(100).WithMessage("Subject cannot exceed 100 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Message body is required.")
            .MaximumLength(10000).WithMessage("Message body cannot exceed 10,000 characters.");
    }
}
