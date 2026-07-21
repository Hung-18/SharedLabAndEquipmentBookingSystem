using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.Cancel;

public sealed record BookingCancelCommand(
    int Id) : IRequest<bool>;

public sealed class BookingCancelCommandHandler : IRequestHandler<BookingCancelCommand, bool>
{
    private readonly IBookingService _service;

    public BookingCancelCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        BookingCancelCommand request,
        CancellationToken cancellationToken)
    {
        await _service.CancelAsync(request.Id, cancellationToken);
        return true;
    }
}
