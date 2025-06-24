using FluentValidation;
using Backend.DTOs;

namespace Backend.DTOs.Validators
{
  
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email є обов'язковим.")
                .EmailAddress().WithMessage("Невірний формат Email.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль є обов'язковим.")
                .MinimumLength(6).WithMessage("Пароль має містити мінімум 6 символів.");
        }
    }
}
