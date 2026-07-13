using Application.DTOs.LabRooms;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ILabRoomService
    {

        Task<List<LabRoomResponse>> GetAllAsync(
            CancellationToken cancellationToken);



        Task<LabRoomDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);



        Task<LabRoomDetailResponse> CreateAsync(
            CreateLabRoomRequest request,
            CancellationToken cancellationToken);



        Task UpdateAsync(
            int id,
            UpdateLabRoomRequest request,
            CancellationToken cancellationToken);
        Task DeleteAsync(
    int id,
    CancellationToken cancellationToken);

    }
}
