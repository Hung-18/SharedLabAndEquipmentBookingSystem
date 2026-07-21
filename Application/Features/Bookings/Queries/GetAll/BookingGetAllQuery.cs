using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Queries.GetAll;

public sealed record BookingGetAllQuery : IRequest<List<BookingResponse>>;

public sealed class BookingGetAllQueryHandler : IRequestHandler<BookingGetAllQuery, List<BookingResponse>>
{
    private readonly IBookingService _service;

    public BookingGetAllQueryHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<List<BookingResponse>> Handle(
        BookingGetAllQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetAllAsync(cancellationToken);
    }
}
