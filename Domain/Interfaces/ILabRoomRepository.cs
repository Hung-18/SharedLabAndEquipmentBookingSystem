using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface ILabRoomRepository : IBaseRepository<LabRoom>
    {
        Task<LabRoom?> GetDetailAsync(int labId, CancellationToken cancellationToken = default);

        Task<LabRoom?> GetByRoomCodeAsync(string roomCode, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LabRoom>> GetByManagerIdAsync(int managerId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LabRoom>> GetAvailableLabRoomsAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LabRoom>> SearchAsync(
            string? keyword,
            LabRoomStatus? status,
            int? managerId,
            CancellationToken cancellationToken = default);

        Task<bool> IsRoomCodeExistsAsync(
            string roomCode,
            int? excludeLabId = null,
            CancellationToken cancellationToken = default);
    }

}
