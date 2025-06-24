using FluentValidation;
using Backend.DTOs;
namespace Backend.DTOs.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email є обов'язковим.")
                .EmailAddress().WithMessage("Невірний формат Email.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль є обов'язковим.")
                .MinimumLength(6).WithMessage("Пароль має містити мінімум 6 символів.")
                .Matches(@"[A-Z]").WithMessage("Пароль має містити принаймні одну велику літеру.")
                .Matches(@"[a-z]").WithMessage("Пароль має містити принаймні одну малу літеру.")
                .Matches(@"\d").WithMessage("Пароль має містити принаймні одну цифру.")
                .Matches(@"[\W]").WithMessage("Пароль має містити принаймні один спеціальний символ.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Повне ім'я є обов'язковим.")
                .MinimumLength(2).WithMessage("Повне ім'я має містити мінімум 2 символи.")
                .MaximumLength(100).WithMessage("Повне ім'я не повинно перевищувати 100 символів.");
        }
    }
}
