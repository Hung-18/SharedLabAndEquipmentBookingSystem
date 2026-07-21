using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands.ResetPassword;

public sealed record UserResetPasswordCommand(
    ResetPasswordRequest ResetPasswordRequest) : IRequest<bool>;

public sealed class UserResetPasswordCommandHandler : IRequestHandler<UserResetPasswordCommand, bool>
{
    private readonly IUserService _service;

    public UserResetPasswordCommandHandler(IUserService service)
    {
        _service = service;
    }

    public Task<bool> Handle(
        UserResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ResetPasswordAsync(request.ResetPasswordRequest, cancellationToken);
    }
}
