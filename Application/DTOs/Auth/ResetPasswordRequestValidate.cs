using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public class ResetPasswordRequestValidate : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordRequestValidate()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email can't empty")
                .EmailAddress().WithMessage("Email must correct format");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Password can't empty")
                .MinimumLength(8).WithMessage("Password must > 8 charactor")
                .Matches("[A-Z]").WithMessage("Password must have less than 1 upper case")
                .Matches("[0-9]").WithMessage("Password must have less than 1 number");
        }
    }
}
