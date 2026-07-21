using Application.DTOs.UsageLogs;
using System;
using System.Collections.Generic;
using System.Text;
using Application.Interfaces;
using MediatR;

namespace Application.Features.UsageLogs.Queries.GetByBookingId;

public sealed record UsageLogGetByBookingIdQuery(
    int BookingId) : IRequest<List<UsageLogResponse>>;

public sealed class UsageLogGetByBookingIdQueryHandler : IRequestHandler<UsageLogGetByBookingIdQuery, List<UsageLogResponse>>
{
    private readonly IUsageLogService _service;

    public UsageLogGetByBookingIdQueryHandler(IUsageLogService service)
    {
        _service = service;
    }

    public Task<List<UsageLogResponse>> Handle(
        UsageLogGetByBookingIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByBookingIdAsync(request.BookingId, cancellationToken);
    }
}
