using FluentValidation;
using Backend.DTOs;
namespace Backend.DTOs.Validators
{
    public class ApiTestScenarioCreateDtoValidator : AbstractValidator<ApiTestScenarioDto>
    {
        public ApiTestScenarioCreateDtoValidator()
        {
            RuleFor(x => x.ScenarioName).NotEmpty().WithMessage("Назва сценарію є обов’язковою.").MaximumLength(150).WithMessage("Максимальна довжина назви сценарію — 150 символів.");

            RuleFor(x => x.TestIds)
                .NotNull().WithMessage("Список ID тестів не може бути null.")
                .Must(ids => ids.Count > 0).WithMessage("Сценарій повинен містити принаймні один тест ID.");
            RuleFor(x => x.CreatedByUserId)
                .NotEmpty().WithMessage("CreatedByUserId є обов’язковим.");
        }
    }

}
    

