using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Queries.GetPenalty;

public sealed record UserManagementGetPenaltyQuery(
    int UserId) : IRequest<UserPenaltyResponse>;

public sealed class UserManagementGetPenaltyQueryHandler : IRequestHandler<UserManagementGetPenaltyQuery, UserPenaltyResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementGetPenaltyQueryHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<UserPenaltyResponse> Handle(
        UserManagementGetPenaltyQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetPenaltyAsync(request.UserId, cancellationToken);
    }
}
