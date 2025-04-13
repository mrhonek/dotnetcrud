using FluentValidation;
using ASPNETCRUD.Application.DTOs;

namespace ASPNETCRUD.Application.Validators
{
    public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
    {
        public RefreshTokenDtoValidator()
        {
            RuleFor(p => p.Token)
                .NotEmpty().WithMessage("{PropertyName} is required");

            RuleFor(p => p.RefreshToken)
                .NotEmpty().WithMessage("{PropertyName} is required");
        }
    }
} 