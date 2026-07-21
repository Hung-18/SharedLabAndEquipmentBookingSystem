using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.SetStatus;

public sealed record UserManagementSetStatusCommand(
    int UserId,
    SetUserStatusRequest Request) : IRequest<UserManagementResponse>;

public sealed class UserManagementSetStatusCommandHandler : IRequestHandler<UserManagementSetStatusCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementSetStatusCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementSetStatusCommand request,
        CancellationToken cancellationToken)
    {
        return _service.SetStatusAsync(request.UserId, request.Request, cancellationToken);
    }
}
