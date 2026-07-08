using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<IReadOnlyList<Notification>> GetByUserIdAsync(
            int userId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default);

        Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    }

}
