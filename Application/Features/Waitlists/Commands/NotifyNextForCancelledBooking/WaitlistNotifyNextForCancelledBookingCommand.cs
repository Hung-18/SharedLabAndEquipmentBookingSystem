using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.NotifyNextForCancelledBooking;

public sealed record WaitlistNotifyNextForCancelledBookingCommand(
    int BookingId) : IRequest<bool>;

public sealed class WaitlistNotifyNextForCancelledBookingCommandHandler : IRequestHandler<WaitlistNotifyNextForCancelledBookingCommand, bool>
{
    private readonly IWaitlistService _service;

    public WaitlistNotifyNextForCancelledBookingCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        WaitlistNotifyNextForCancelledBookingCommand request,
        CancellationToken cancellationToken)
    {
        await _service.NotifyNextForCancelledBookingAsync(request.BookingId, cancellationToken);
        return true;
    }
}
