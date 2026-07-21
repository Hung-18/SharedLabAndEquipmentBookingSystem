using Application.DTOs.Booking;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Bookings.Queries.SuggestAlternativeSlots;

public sealed record BookingSuggestAlternativeSlotsQuery(
    AlternativeSlotRequest Request) : IRequest<List<SuggestedSlotResponse>>;

public sealed class BookingSuggestAlternativeSlotsQueryHandler : IRequestHandler<BookingSuggestAlternativeSlotsQuery, List<SuggestedSlotResponse>>
{
    private readonly IBookingService _service;

    public BookingSuggestAlternativeSlotsQueryHandler(IBookingService service)
    {
        _service = service;
    }

    public Task<List<SuggestedSlotResponse>> Handle(
        BookingSuggestAlternativeSlotsQuery request,
        CancellationToken cancellationToken)
    {
        return _service.SuggestAlternativeSlotsAsync(request.Request, cancellationToken);
    }
}
