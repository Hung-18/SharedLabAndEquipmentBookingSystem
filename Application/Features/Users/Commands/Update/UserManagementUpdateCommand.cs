using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.Update;

public sealed record UserManagementUpdateCommand(
    int UserId,
    UpdateUserRequest Request) : IRequest<UserManagementResponse>;

public sealed class UserManagementUpdateCommandHandler : IRequestHandler<UserManagementUpdateCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementUpdateCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementUpdateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.UpdateAsync(request.UserId, request.Request, cancellationToken);
    }
}
