using Application.DTOs.Booking;

namespace Application.Exceptions
{
    public class ResourceUnavailableException : InvalidOperationException
    {
        public ResourceUnavailableException(
            string message,
            IReadOnlyList<SuggestedSlotResponse> suggestedSlots)
            : base(message)
        {
            SuggestedSlots = suggestedSlots;
        }

        public IReadOnlyList<SuggestedSlotResponse> SuggestedSlots { get; }
    }
}
