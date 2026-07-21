using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.Lock;

public sealed record UserManagementLockCommand(
    int UserId) : IRequest<UserManagementResponse>;

public sealed class UserManagementLockCommandHandler : IRequestHandler<UserManagementLockCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementLockCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementLockCommand request,
        CancellationToken cancellationToken)
    {
        return _service.LockAsync(request.UserId, cancellationToken);
    }
}
