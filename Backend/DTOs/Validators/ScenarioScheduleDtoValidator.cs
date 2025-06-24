using FluentValidation;
using Backend.DTOs;
using System.Text.RegularExpressions;
namespace Backend.DTOs.Validators
{
    

    public class ScenarioScheduleDtoValidator : AbstractValidator<ScenarioScheduleDto>
    {
        public ScenarioScheduleDtoValidator()
        {
            RuleFor(x => x.ScenarioId)
                .NotEmpty().WithMessage("ScenarioId є обов'язковим.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId є обов'язковим.");

            RuleFor(x => x.StartTime)
                .Must(date => date > DateTimeOffset.UtcNow)
                .WithMessage("StartTime має бути у майбутньому.");

            RuleFor(x => x.CronExpression)
                .NotEmpty().WithMessage("CronExpression є обов'язковим.")
                .Must(IsValidCronExpression).WithMessage("Невірний формат Cron Expression.");
        }

        private bool IsValidCronExpression(string cron)
        {
            if (string.IsNullOrWhiteSpace(cron))
                return false;
            var parts = cron.Trim().Split(' ');
            return parts.Length == 6 && parts.All(p => !string.IsNullOrWhiteSpace(p));
        }
    }
}
