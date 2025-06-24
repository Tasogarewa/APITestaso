using FluentValidation;
using Backend.DTOs;
namespace Backend.DTOs.Validators
{
    public class ApiTestDtoValidator : AbstractValidator<ApiTestDto>
    {
        public ApiTestDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва тесту є обов'язковою.")
                .MaximumLength(100).WithMessage("Максимальна довжина назви — 100 символів.");

            RuleFor(x => x.Method)
                .NotEmpty().WithMessage("HTTP метод є обов'язковим.")
                .Must(method => new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" }
                    .Contains(method.ToUpper()))
                .WithMessage("Метод повинен бути одним із: GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.");

            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("URL є обов'язковим.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("URL має бути коректним абсолютним URI.");

            RuleFor(x => x.TimeoutSeconds)
                .InclusiveBetween(1, 300)
                .When(x => x.TimeoutSeconds.HasValue)
                .WithMessage("TimeoutSeconds має бути в межах від 1 до 300 секунд.");

            RuleFor(x => x.ExpectedStatusCode)
                .InclusiveBetween(100, 599)
                .WithMessage("ExpectedStatusCode має бути коректним HTTP кодом статусу (100-599).");

            RuleFor(x => x.CreatedByUserId)
                .NotEmpty().WithMessage("CreatedByUserId є обов'язковим.");
        }
    }
}
