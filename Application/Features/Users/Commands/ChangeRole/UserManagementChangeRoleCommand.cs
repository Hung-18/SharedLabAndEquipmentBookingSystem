using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.ChangeRole;

public sealed record UserManagementChangeRoleCommand(
    int UserId,
    ChangeUserRoleRequest Request) : IRequest<UserManagementResponse>;

public sealed class UserManagementChangeRoleCommandHandler : IRequestHandler<UserManagementChangeRoleCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementChangeRoleCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementChangeRoleCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ChangeRoleAsync(request.UserId, request.Request, cancellationToken);
    }
}
