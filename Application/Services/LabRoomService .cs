using Application.DTOs.LabRooms;
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Domain.Interfaces;

public class LabRoomService : ILabRoomService
{
    private readonly ILabRoomRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public LabRoomService(ILabRoomRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<LabRoomDetailResponse> CreateAsync(
     CreateLabRoomRequest request,
     CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Kiểm tra ManagerId có tồn tại trong Users không
        var manager = await _unitOfWork.Users.GetUserByIdAsync(
            request.ManagerId,
            cancellationToken);

        if (manager is null)
        {
            throw new KeyNotFoundException(
                $"Không tìm thấy người quản lý có ID {request.ManagerId}.");
        }

        // Theo ERD, người quản lý phòng phải có role LabManager
        if (manager.Role?.RoleName != RoleName.LabManager)
        {
            throw new ArgumentException(
                $"User có ID {request.ManagerId} không có vai trò LabManager.");
        }

        if (manager.Status != UserStatus.Active)
        {
            throw new ArgumentException(
                $"Người quản lý có ID {request.ManagerId} hiện không hoạt động.");
        }

        string roomCode = request.RoomCode.Trim();

        bool roomCodeExists = await _repository.IsRoomCodeExistsAsync(
            roomCode,
            null,
            cancellationToken);

        if (roomCodeExists)
        {
            throw new InvalidOperationException(
                $"Mã phòng '{roomCode}' đã tồn tại.");
        }

        var room = new LabRoom(
            request.ManagerId,
            request.LabName,
            roomCode,
            request.Location,
            request.Capacity,
            request.ImageUrl,
            request.UsageGuideline);

        await _repository.AddAsync(room, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdRoom = await _repository.GetDetailAsync(
            room.LabId,
            cancellationToken);

        if (createdRoom is null)
        {
            throw new InvalidOperationException(
                $"Đã tạo phòng ID {room.LabId} nhưng không thể đọc lại dữ liệu.");
        }

        return new LabRoomDetailResponse
        {
            LabId = createdRoom.LabId,
            LabName = createdRoom.LabName,
            RoomCode = createdRoom.RoomCode,
            Location = createdRoom.Location,
            Capacity = createdRoom.Capacity,
            ImageUrl = createdRoom.ImageUrl,
            UsageGuideline = createdRoom.UsageGuideline,
            Status = createdRoom.Status.ToString(),
            ManagerName = createdRoom.Manager?.FullName
        };
    }

    public async Task<List<LabRoomResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var rooms = await _repository.GetAllAsync(cancellationToken);

        return rooms
            .Select(room => new LabRoomResponse
            {
                LabId = room.LabId,
                LabName = room.LabName,
                RoomCode = room.RoomCode,
                Location = room.Location,
                Capacity = room.Capacity,
                Status = room.Status.ToString()
            })
            .ToList();
    }

    public async Task<LabRoomDetailResponse?> GetByIdAsync(
     int id,
     CancellationToken cancellationToken)
    {
        var room = await _repository.GetDetailAsync(
            id,
            cancellationToken);

        if (room == null)
        {
            return null;
        }

        return new LabRoomDetailResponse
        {
            LabId = room.LabId,
            LabName = room.LabName,
            RoomCode = room.RoomCode,
            Location = room.Location,
            Capacity = room.Capacity,
            ImageUrl = room.ImageUrl,
            UsageGuideline = room.UsageGuideline,
            Status = room.Status.ToString(),
            ManagerName = room.Manager?.FullName
        };
    }

    public async Task UpdateAsync(
     int id,
     UpdateLabRoomRequest request,
     CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var room = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException(
                $"Không tìm thấy phòng lab có ID {id}.");
        }

        room.UpdateDetails(
            request.LabName,
            request.Location,
            request.Capacity,
            request.ImageUrl,
            request.UsageGuideline);

        _repository.Update(room);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
    //soft delete không bị xóa khỏi database chỉ đổi status thành Inactive, không hiển thị trong danh sách phòng lab
    public async Task DeleteAsync(
    int id,
    CancellationToken cancellationToken)
    {
        var room = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (room is null)
        {
            throw new KeyNotFoundException(
                $"Không tìm thấy phòng lab có ID {id}.");
        }

        room.Deactivate();

        _repository.Update(room);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

}