using FluentValidation;
using Backend.DTOs;
namespace Backend.DTOs.Validators
{
    

    public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
    {
        public UpdateProfileDtoValidator()
        {
            When(x => !string.IsNullOrEmpty(x.Email), () =>
            {
                RuleFor(x => x.Email)
                    .EmailAddress().WithMessage("Невірний формат Email.");
            });

            When(x => !string.IsNullOrEmpty(x.UserName), () =>
            {
                RuleFor(x => x.UserName)
                    .MinimumLength(2).WithMessage("Ім'я користувача має містити мінімум 2 символи.")
                    .MaximumLength(50).WithMessage("Ім'я користувача не повинно перевищувати 50 символів.");
            });

            When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
            {
                RuleFor(x => x.PhoneNumber)
                    .Matches(@"^\+?\d{7,15}$").WithMessage("Невірний формат номера телефону.");
            });
        }
    }
}
