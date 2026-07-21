using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands.RefreshToken;

public sealed record UserRefreshTokenCommand(
    RefreshTokenRequest RefreshToken) : IRequest<AuthResponseDTO?>;

public sealed class UserRefreshTokenCommandHandler : IRequestHandler<UserRefreshTokenCommand, AuthResponseDTO?>
{
    private readonly IUserService _service;

    public UserRefreshTokenCommandHandler(IUserService service)
    {
        _service = service;
    }

    public Task<AuthResponseDTO?> Handle(
        UserRefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        return _service.RefreshTokenAsync(request.RefreshToken, cancellationToken);
    }
}
