using EnterpriseApp.Application.DTOs.Library;
using FluentValidation;

namespace EnterpriseApp.Application.Validators.Library;

public class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Book title is required.")
            .MaximumLength(200).WithMessage("Book title cannot exceed 200 characters.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(150).WithMessage("Author name cannot exceed 150 characters.");

        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("ISBN is required.")
            .Matches(@"^(?=(?:\D*\d){10}(?:(?:\D*\d){3})?$)[\d-]+$")
            .WithMessage("A valid ISBN-10 or ISBN-13 format is required.");

        RuleFor(x => x.PublicationYear)
            .InclusiveBetween(1440, DateTime.UtcNow.Year + 1)
            .WithMessage($"Publication year must be between 1440 and {DateTime.UtcNow.Year + 1}.");
    }
}
