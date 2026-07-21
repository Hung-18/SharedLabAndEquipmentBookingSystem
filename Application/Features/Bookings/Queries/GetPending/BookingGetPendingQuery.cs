using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Queries.GetPending;

public sealed record BookingGetPendingQuery : IRequest<List<BookingResponse>>;

public sealed class BookingGetPendingQueryHandler : IRequestHandler<BookingGetPendingQuery, List<BookingResponse>>
{
    private readonly IBookingService _service;

    public BookingGetPendingQueryHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<List<BookingResponse>> Handle(
        BookingGetPendingQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetPendingAsync(cancellationToken);
    }
}
