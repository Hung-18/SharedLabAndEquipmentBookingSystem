using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands.Logout;

public sealed record AuthLogoutCommand(
    string RefreshToken) : IRequest<bool>;

public sealed class AuthLogoutCommandHandler : IRequestHandler<AuthLogoutCommand, bool>
{
    private readonly IAuthService _service;

    public AuthLogoutCommandHandler(IAuthService service)
    {
        _service = service;
    }

    public Task<bool> Handle(
        AuthLogoutCommand request,
        CancellationToken cancellationToken)
    {
        return _service.LogoutAsync(request.RefreshToken, cancellationToken);
    }
}
