using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.Reject;

public sealed record BookingRejectCommand(
    int Id,
    RejectBookingRequest Request) : IRequest<bool>;

public sealed class BookingRejectCommandHandler : IRequestHandler<BookingRejectCommand, bool>
{
    private readonly IBookingService _service;

    public BookingRejectCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        BookingRejectCommand request,
        CancellationToken cancellationToken)
    {
        await _service.RejectAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
