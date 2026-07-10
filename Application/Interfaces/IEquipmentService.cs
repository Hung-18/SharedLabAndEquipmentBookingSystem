using Application.DTOs.Equipments;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IEquipmentService
    {
        Task<List<EquipmentResponse>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<EquipmentDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task<List<EquipmentResponse>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken);

        Task<EquipmentDetailResponse> CreateAsync(
            CreateEquipmentRequest request,
            CancellationToken cancellationToken);

        Task UpdateAsync(
            int id,
            UpdateEquipmentRequest request,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            int id,
            CancellationToken cancellationToken);
    }
}
