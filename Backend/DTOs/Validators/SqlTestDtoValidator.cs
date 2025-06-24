using FluentValidation;
using Backend.DTOs;
using System.Text.Json;

public class SqlTestDtoValidator : AbstractValidator<SqlTestDto>
{
    public SqlTestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва тесту є обов'язковою.")
            .MaximumLength(150).WithMessage("Максимальна довжина назви — 150 символів.");

        RuleFor(x => x.SqlQuery)
            .NotEmpty().WithMessage("SQL-запит є обов'язковим.")
            .MaximumLength(10000).WithMessage("SQL-запит не може перевищувати 10000 символів.");

        RuleFor(x => x.TestType)
            .IsInEnum().WithMessage("TestType має бути валідним значенням.");

        RuleFor(x => x.DatabaseConnectionName)
            .NotEmpty().WithMessage("Ім'я підключення до бази даних є обов'язковим.")
            .MaximumLength(200).WithMessage("Ім'я підключення не повинно перевищувати 100 символів.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId є обов'язковим.");

        RuleFor(x => x.ExpectedJson)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrWhiteSpace(x.ExpectedJson))
            .WithMessage("ExpectedJson повинен бути валідним JSON.");

        RuleFor(x => x.ParametersJson)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrWhiteSpace(x.ParametersJson))
            .WithMessage("ParametersJson повинен бути валідним JSON.");
    }

    private bool BeValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}