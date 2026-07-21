using Application.DTOs.Roles;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Roles.Queries.GetById;

public sealed record RoleGetByIdQuery(
    int RoleId) : IRequest<RoleResponse?>;

public sealed class RoleGetByIdQueryHandler : IRequestHandler<RoleGetByIdQuery, RoleResponse?>
{
    private readonly IRoleService _service;

    public RoleGetByIdQueryHandler(IRoleService service)
    {
        _service = service;
    }

    public Task<RoleResponse?> Handle(
        RoleGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.RoleId, cancellationToken);
    }
}
