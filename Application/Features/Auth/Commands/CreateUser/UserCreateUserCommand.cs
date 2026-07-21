using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Commands.CreateUser;

public sealed record UserCreateUserCommand(
    CreateUserDTO CreateUserDTO) : IRequest<UserDTO>;

public sealed class UserCreateUserCommandHandler : IRequestHandler<UserCreateUserCommand, UserDTO>
{
    private readonly IUserService _service;

    public UserCreateUserCommandHandler(IUserService service)
    {
        _service = service;
    }

    public Task<UserDTO> Handle(
        UserCreateUserCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateUserAsync(request.CreateUserDTO, cancellationToken);
    }
}
