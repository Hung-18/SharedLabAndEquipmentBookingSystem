using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Queries.GetAll;

public sealed record UsageLogGetAllQuery : IRequest<List<UsageLogResponse>>;

public sealed class UsageLogGetAllQueryHandler : IRequestHandler<UsageLogGetAllQuery, List<UsageLogResponse>>
{
    private readonly IUsageLogService _service;

    public UsageLogGetAllQueryHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<List<UsageLogResponse>> Handle(
        UsageLogGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
