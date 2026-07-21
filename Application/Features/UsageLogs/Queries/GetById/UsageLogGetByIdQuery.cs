using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Queries.GetById;

public sealed record UsageLogGetByIdQuery(
    int Id) : IRequest<UsageLogResponse?>;

public sealed class UsageLogGetByIdQueryHandler : IRequestHandler<UsageLogGetByIdQuery, UsageLogResponse?>
{
    private readonly IUsageLogService _service;

    public UsageLogGetByIdQueryHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<UsageLogResponse?> Handle(
        UsageLogGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
