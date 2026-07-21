using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.MarkNoShow;

public sealed record BookingMarkNoShowCommand(
    int Id) : IRequest<bool>;

public sealed class BookingMarkNoShowCommandHandler : IRequestHandler<BookingMarkNoShowCommand, bool>
{
    private readonly IBookingService _service;

    public BookingMarkNoShowCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        BookingMarkNoShowCommand request,
        CancellationToken cancellationToken)
    {
        await _service.MarkNoShowAsync(request.Id, cancellationToken);
        return true;
    }
}
