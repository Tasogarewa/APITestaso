
using Backend.DTOs;
using FluentValidation;

namespace Backend.Validators
{
    public class TestResultDtoValidator : AbstractValidator<TestResultDto>
    {
        public TestResultDtoValidator()
        {
            RuleFor(x => x.ExecutedByUserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.ExecutedAt)
                .NotEmpty().WithMessage("Execution date is required.")
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Execution date cannot be in the future.");

            RuleFor(x => x.DurationMilliseconds)
                .GreaterThanOrEqualTo(0).When(x => x.DurationMilliseconds.HasValue)
                .WithMessage("Duration must be non-negative.");

            RuleFor(x => x)
                .Must(x => x.ApiTestId.HasValue || x.SqlTestId.HasValue)
                .WithMessage("Either ApiTestId or SqlTestId must be provided.");
        }
    }
}