using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Auth.Queries.GetUserByIdService;

public sealed record UserGetUserByIdServiceQuery : IRequest<UserDTO?>;

public sealed class UserGetUserByIdServiceQueryHandler : IRequestHandler<UserGetUserByIdServiceQuery, UserDTO?>
{
    private readonly IUserService _service;

    public UserGetUserByIdServiceQueryHandler(IUserService service)
    {
        _service = service;
    }

    public Task<UserDTO?> Handle(
        UserGetUserByIdServiceQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetUserByIdServiceAsync(cancellationToken);
    }
}
