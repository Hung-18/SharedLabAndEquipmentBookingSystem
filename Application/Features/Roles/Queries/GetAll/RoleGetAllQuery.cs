using Application.DTOs.Roles;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Roles.Queries.GetAll;

public sealed record RoleGetAllQuery : IRequest<List<RoleResponse>>;

public sealed class RoleGetAllQueryHandler : IRequestHandler<RoleGetAllQuery, List<RoleResponse>>
{
    private readonly IRoleService _service;

    public RoleGetAllQueryHandler(IRoleService service)
    {
        _service = service;
    }

    public Task<List<RoleResponse>> Handle(
        RoleGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
