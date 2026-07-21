using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.Complete;

public sealed record BookingCompleteCommand(
    int Id) : IRequest<bool>;

public sealed class BookingCompleteCommandHandler : IRequestHandler<BookingCompleteCommand, bool>
{
    private readonly IBookingService _service;

    public BookingCompleteCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        BookingCompleteCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CompleteAsync(request.Id, cancellationToken);
        return true;
    }
}
