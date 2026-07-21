using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands.ForgotPassword;

public sealed record UserForgotPasswordCommand(
    string Email) : IRequest<bool>;

public sealed class UserForgotPasswordCommandHandler : IRequestHandler<UserForgotPasswordCommand, bool>
{
    private readonly IUserService _service;

    public UserForgotPasswordCommandHandler(IUserService service)
    {
        _service = service;
    }

    public Task<bool> Handle(
        UserForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ForgotPasswordAsync(request.Email, cancellationToken);
    }
}
