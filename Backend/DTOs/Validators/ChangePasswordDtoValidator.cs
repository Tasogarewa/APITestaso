using FluentValidation;
using Backend.DTOs;
namespace Backend.DTOs.Validators
{
    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Поточний пароль є обов'язковим.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Новий пароль є обов'язковим.")
                .MinimumLength(6).WithMessage("Новий пароль має містити мінімум 6 символів.")
                .Matches(@"[A-Z]").WithMessage("Новий пароль має містити принаймні одну велику літеру.")
                .Matches(@"[a-z]").WithMessage("Новий пароль має містити принаймні одну малу літеру.")
                .Matches(@"\d").WithMessage("Новий пароль має містити принаймні одну цифру.")
                .Matches(@"[\W]").WithMessage("Новий пароль має містити принаймні один спеціальний символ.");
        }
    }
}
