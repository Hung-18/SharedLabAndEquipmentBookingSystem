using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.Deactivate;

public sealed record UserManagementDeactivateCommand(
    int UserId) : IRequest<UserManagementResponse>;

public sealed class UserManagementDeactivateCommandHandler : IRequestHandler<UserManagementDeactivateCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementDeactivateCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementDeactivateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.DeactivateAsync(request.UserId, cancellationToken);
    }
}
