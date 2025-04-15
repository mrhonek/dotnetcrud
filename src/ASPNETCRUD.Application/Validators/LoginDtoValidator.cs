using FluentValidation;
using ASPNETCRUD.Application.DTOs;

namespace ASPNETCRUD.Application.Validators
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(p => p.Username)
                .NotEmpty().WithMessage("Username or email is required");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
} 