using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Queries.GetById;

public sealed record BookingGetByIdQuery(
    int Id) : IRequest<BookingDetailResponse?>;

public sealed class BookingGetByIdQueryHandler : IRequestHandler<BookingGetByIdQuery, BookingDetailResponse?>
{
    private readonly IBookingService _service;

    public BookingGetByIdQueryHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<BookingDetailResponse?> Handle(
        BookingGetByIdQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
