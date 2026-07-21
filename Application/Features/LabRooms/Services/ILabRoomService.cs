using Application.DTOs.LabRooms;

namespace Application.Interfaces
{
    public interface ILabRoomService
    {
        Task<PagedLabRoomResponse> SearchAsync(
            LabRoomSearchRequest request,
            CancellationToken cancellationToken);
        Task<List<LabRoomResponse>> GetAllAsync(CancellationToken cancellationToken);
        Task<LabRoomDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<LabRoomDetailResponse> CreateAsync(
            CreateLabRoomRequest request,
            CancellationToken cancellationToken);
        Task UpdateAsync(int id, UpdateLabRoomRequest request, CancellationToken cancellationToken);
        Task ChangeManagerAsync(
            int id,
            ChangeLabRoomManagerRequest request,
            CancellationToken cancellationToken);
        Task DeleteAsync(int id, CancellationToken cancellationToken);
    }
}
