using FluentValidation;
using ASPNETCRUD.Application.DTOs;

namespace ASPNETCRUD.Application.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(p => p.Username)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MinimumLength(3).WithMessage("{PropertyName} must be at least 3 characters")
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters");

            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .EmailAddress().WithMessage("{PropertyName} must be a valid email address");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MinimumLength(6).WithMessage("{PropertyName} must be at least 6 characters")
                .Matches("[A-Z]").WithMessage("{PropertyName} must contain at least 1 uppercase letter")
                .Matches("[a-z]").WithMessage("{PropertyName} must contain at least 1 lowercase letter")
                .Matches("[0-9]").WithMessage("{PropertyName} must contain at least 1 number")
                .Matches("[^a-zA-Z0-9]").WithMessage("{PropertyName} must contain at least 1 special character");

            RuleFor(p => p.ConfirmPassword)
                .Equal(p => p.Password).WithMessage("Passwords do not match");

            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters");

            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MaximumLength(50).WithMessage("{PropertyName} must not exceed 50 characters");
        }
    }
} 