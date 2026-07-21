using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Queries.GetCalendar;

public sealed record BookingGetCalendarQuery(
    DateTime From,
    DateTime To,
    int? LabId,
    int? EquipmentId) : IRequest<List<CalendarEventResponse>>;

public sealed class BookingGetCalendarQueryHandler : IRequestHandler<BookingGetCalendarQuery, List<CalendarEventResponse>>
{
    private readonly IBookingService _service;

    public BookingGetCalendarQueryHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<List<CalendarEventResponse>> Handle(
        BookingGetCalendarQuery request,
        CancellationToken cancellationToken)
    {
        return _service.GetCalendarAsync(request.From, request.To, request.LabId, request.EquipmentId, cancellationToken);
    }
}
