using Application.DTOs.Maintenances;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IMaintenanceService
    {
        Task<List<MaintenanceResponse>> GetAllAsync(
            CancellationToken cancellationToken);

        Task<MaintenanceDetailResponse?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken);

        Task<List<MaintenanceResponse>> GetByLabIdAsync(
            int labId,
            CancellationToken cancellationToken);

        Task<List<MaintenanceResponse>> GetByEquipmentIdAsync(
            int equipmentId,
            CancellationToken cancellationToken);

        Task<MaintenanceDetailResponse> CreateAsync(
            CreateMaintenanceRequest request,
            CancellationToken cancellationToken);

        Task UpdateAsync(
            int id,
            UpdateMaintenanceRequest request,
            CancellationToken cancellationToken);

        Task StartAsync(
            int id,
            CancellationToken cancellationToken);

        Task CompleteAsync(
            int id,
            CancellationToken cancellationToken);

        Task CancelAsync(
            int id,
            CancellationToken cancellationToken);

        Task CancelSeriesAsync(
            int id,
            CancellationToken cancellationToken);
    }
}
