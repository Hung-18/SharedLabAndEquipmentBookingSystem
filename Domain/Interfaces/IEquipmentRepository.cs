using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IEquipmentRepository : IBaseRepository<Equipment>
    {
        Task<Equipment?> GetDetailAsync(int equipmentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Equipment>> GetByLabIdAsync(int labId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Equipment>> GetAvailableByLabIdAsync(int labId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Equipment>> SearchAsync(
            string? keyword,
            int? labId,
            EquipmentStatus? status,
            CancellationToken cancellationToken = default);
    }

}
