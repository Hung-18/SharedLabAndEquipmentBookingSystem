using Application.DTOs.Users;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Queries.Search;

public sealed record UserManagementSearchQuery(
    string? Keyword,
    RoleName? RoleName,
    int? DepartmentId,
    UserStatus? Status,
    int PageNumber,
    int PageSize) : IRequest<PagedUserResponse>;

public sealed class UserManagementSearchQueryHandler : IRequestHandler<UserManagementSearchQuery, PagedUserResponse>
{
    private readonly IUserManagementService _service;

    public UserManagementSearchQueryHandler(IUserManagementService service)
    {
        _service = service;
    }

    public Task<PagedUserResponse> Handle(
        UserManagementSearchQuery request,
        CancellationToken cancellationToken)
    {
        return _service.SearchAsync(request.Keyword, request.RoleName, request.DepartmentId, request.Status, request.PageNumber, request.PageSize, cancellationToken);
    }
}
