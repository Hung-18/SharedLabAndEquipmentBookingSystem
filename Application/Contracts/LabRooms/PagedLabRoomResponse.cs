namespace Application.DTOs.LabRooms
{
    public class PagedLabRoomResponse
    {
        public IReadOnlyList<LabRoomResponse> Items { get; set; } = Array.Empty<LabRoomResponse>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}
