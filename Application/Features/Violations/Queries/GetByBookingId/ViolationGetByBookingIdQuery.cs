using Application.DTOs.Violations;
using Domain;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Violations.Queries.GetByBookingId;

public sealed record ViolationGetByBookingIdQuery(
    int BookingId) : IRequest<List<ViolationResponse>>;

public sealed class ViolationGetByBookingIdQueryHandler : IRequestHandler<ViolationGetByBookingIdQuery, List<ViolationResponse>>
{
    private readonly IViolationService _service;

    public ViolationGetByBookingIdQueryHandler(IViolationService service)
    {
        _service = service;
    }

    public Task<List<ViolationResponse>> Handle(
        ViolationGetByBookingIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByBookingIdAsync(request.BookingId, cancellationToken);
    }
}
