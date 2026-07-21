using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.Update;

public sealed record BookingUpdateCommand(
    int Id,
    UpdateBookingRequest Request) : IRequest<bool>;

public sealed class BookingUpdateCommandHandler : IRequestHandler<BookingUpdateCommand, bool>
{
    private readonly IBookingService _service;

    public BookingUpdateCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public async Task<bool> Handle(
        BookingUpdateCommand request,
        CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(request.Id, request.Request, cancellationToken);
        return true;
    }
}
