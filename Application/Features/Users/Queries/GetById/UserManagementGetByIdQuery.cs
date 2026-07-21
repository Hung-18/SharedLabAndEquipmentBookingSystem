using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Queries.GetById;

public sealed record UserManagementGetByIdQuery(
    int UserId) : IRequest<UserManagementResponse?>;

public sealed class UserManagementGetByIdQueryHandler : IRequestHandler<UserManagementGetByIdQuery, UserManagementResponse?>
{
    private readonly IUserManagementService _service;

    public UserManagementGetByIdQueryHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserManagementResponse?> Handle(
        UserManagementGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.UserId, cancellationToken);
    }
}
