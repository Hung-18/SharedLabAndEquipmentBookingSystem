using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands.Login;

public sealed record AuthLoginCommand(
    LoginRequestDTO LoginDTO) : IRequest<AuthResponseDTO?>;

public sealed class AuthLoginCommandHandler : IRequestHandler<AuthLoginCommand, AuthResponseDTO?>
{
    private readonly IAuthService _service;

    public AuthLoginCommandHandler(IAuthService service)
    {
        _service = service;
    }

    public Task<AuthResponseDTO?> Handle(
        AuthLoginCommand request,
        CancellationToken cancellationToken)
    {
        return _service.LoginAsync(request.LoginDTO, cancellationToken);
    }
}
