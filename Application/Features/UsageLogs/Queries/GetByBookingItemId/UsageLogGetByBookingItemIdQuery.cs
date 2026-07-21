using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Queries.GetByBookingItemId;

public sealed record UsageLogGetByBookingItemIdQuery(
    int BookingItemId) : IRequest<List<UsageLogResponse>>;

public sealed class UsageLogGetByBookingItemIdQueryHandler : IRequestHandler<UsageLogGetByBookingItemIdQuery, List<UsageLogResponse>>
{
    private readonly IUsageLogService _service;

    public UsageLogGetByBookingItemIdQueryHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<List<UsageLogResponse>> Handle(
        UsageLogGetByBookingItemIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByBookingItemIdAsync(request.BookingItemId, cancellationToken);
    }
}
