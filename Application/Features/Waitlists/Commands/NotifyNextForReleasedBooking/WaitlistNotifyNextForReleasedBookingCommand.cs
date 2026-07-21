using Application.DTOs.Waitlists;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Waitlists.Commands.NotifyNextForReleasedBooking;

public sealed record WaitlistNotifyNextForReleasedBookingCommand(
    int BookingId) : IRequest<bool>;

public sealed class WaitlistNotifyNextForReleasedBookingCommandHandler : IRequestHandler<WaitlistNotifyNextForReleasedBookingCommand, bool>
{
    private readonly IWaitlistService _service;

    public WaitlistNotifyNextForReleasedBookingCommandHandler(IWaitlistService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        WaitlistNotifyNextForReleasedBookingCommand request,
        CancellationToken cancellationToken)
    {
        await _service.NotifyNextForReleasedBookingAsync(request.BookingId, cancellationToken);
        return true;
    }
}
