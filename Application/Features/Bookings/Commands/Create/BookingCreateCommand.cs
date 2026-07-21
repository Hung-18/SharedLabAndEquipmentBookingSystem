using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Commands.Create;

public sealed record BookingCreateCommand(
    CreateBookingRequest Request) : IRequest<BookingDetailResponse>;

public sealed class BookingCreateCommandHandler : IRequestHandler<BookingCreateCommand, BookingDetailResponse>
{
    private readonly IBookingService _service;

    public BookingCreateCommandHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<BookingDetailResponse> Handle(
        BookingCreateCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CreateAsync(request.Request, cancellationToken);
    }
}
