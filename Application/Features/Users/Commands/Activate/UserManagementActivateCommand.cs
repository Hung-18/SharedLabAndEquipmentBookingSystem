using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.Activate;

public sealed record UserManagementActivateCommand(
    int UserId) : IRequest<UserManagementResponse>;

public sealed class UserManagementActivateCommandHandler : IRequestHandler<UserManagementActivateCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementActivateCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementActivateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ActivateAsync(request.UserId, cancellationToken);
    }
}
