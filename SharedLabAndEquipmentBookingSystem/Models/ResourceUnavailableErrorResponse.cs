using Application.DTOs.Booking;

namespace API.Models;

public sealed record ResourceUnavailableErrorResponse(
    int StatusCode,
    string Message,
    IReadOnlyList<SuggestedSlotResponse> SuggestedSlots,
    DateTime Timestamp);
