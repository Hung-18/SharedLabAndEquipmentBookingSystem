using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.Approve;

public sealed record BookingApproveCommand(
    int Id) : IRequest<bool>;

public sealed class BookingApproveCommandHandler : IRequestHandler<BookingApproveCommand, bool>
{
    private readonly IBookingService _service;

    public BookingApproveCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        BookingApproveCommand request,
        CancellationToken cancellationToken)
    {
        await _service.ApproveAsync(request.Id, cancellationToken);
        return true;
    }
}
