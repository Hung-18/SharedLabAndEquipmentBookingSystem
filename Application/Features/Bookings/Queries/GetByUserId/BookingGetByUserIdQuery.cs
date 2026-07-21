using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Queries.GetByUserId;

public sealed record BookingGetByUserIdQuery(
    int UserId) : IRequest<List<BookingResponse>>;

public sealed class BookingGetByUserIdQueryHandler : IRequestHandler<BookingGetByUserIdQuery, List<BookingResponse>>
{
    private readonly IBookingService _service;

    public BookingGetByUserIdQueryHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<List<BookingResponse>> Handle(
        BookingGetByUserIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
