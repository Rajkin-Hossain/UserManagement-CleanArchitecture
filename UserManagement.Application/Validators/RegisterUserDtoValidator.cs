using FluentValidation;
using System;
using System.Linq;
using UserManagement.Application.DTOs;

namespace UserManagement.Application.Validators
{
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().MinimumLength(3).MaximumLength(20)
                .Must(u => !IsReservedWord(u)).WithMessage("Username is reserved.");

            RuleFor(x => x.FullName)
                .NotEmpty().Length(2, 80)
                .Matches(@"^[a-zA-Z\s]*$").WithMessage("Full name must not contain symbols or digits.");

            RuleFor(x => x.Email).NotEmpty().EmailAddress();

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid E.164 phone format.")
                .Must(p => !p.StartsWith("+880") || (p.Length == 14 && p.Substring(4, 3) is "171" or "181" or "191" or "161" or "131" or "141" or "151"))
                .WithMessage("Invalid operator prefix for Bangladesh.");

            RuleFor(x => x.Password)
                .NotEmpty().MinimumLength(12)
                .Matches(@"[A-Z]").WithMessage("Password must contain uppercase.")
                .Matches(@"[a-z]").WithMessage("Password must contain lowercase.")
                .Matches(@"[0-9]").WithMessage("Password must contain digit.")
                .Matches(@"[\!\@\#\$\%\^\&\*]").WithMessage("Password must contain symbol.");
            
            RuleFor(x => x.BirthDate)
                .Must(date => IsAtLeast(date, 13)).WithMessage("User must be at least 13 years old.");

            RuleFor(x => x.TermsVersion).NotEmpty();
        }

        private bool IsReservedWord(string username)
        {
            var reserved = new[] { "admin", "support", "system", "root" };
            return reserved.Contains(username.ToLower());
        }

        private bool IsAtLeast(DateTime birthDate, int minAge)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age >= minAge;
        }
    }
}
