using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands.ChangeDepartment;

public sealed record UserManagementChangeDepartmentCommand(
    int UserId,
    ChangeUserDepartmentRequest Request) : IRequest<UserManagementResponse>;

public sealed class UserManagementChangeDepartmentCommandHandler : IRequestHandler<UserManagementChangeDepartmentCommand, UserManagementResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementChangeDepartmentCommandHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse> Handle(
        UserManagementChangeDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ChangeDepartmentAsync(request.UserId, request.Request, cancellationToken);
    }
}
