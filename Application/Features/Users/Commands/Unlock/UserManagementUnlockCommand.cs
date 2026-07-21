using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.Unlock;

public sealed record UserManagementUnlockCommand(
    int UserId) : IRequest<UserManagementResponse>;

public sealed class UserManagementUnlockCommandHandler : IRequestHandler<UserManagementUnlockCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementUnlockCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementUnlockCommand request,
        CancellationToken cancellationToken)
    {
        return _service.UnlockAsync(request.UserId, cancellationToken);
    }
}
