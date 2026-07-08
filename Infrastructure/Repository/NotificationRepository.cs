using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
            int userId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            return await Context.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountUnreadAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Notifications
                .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);
        }

        public async Task MarkAllAsReadAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var notifications = await Context.Notifications
                .Where(x => x.UserId == userId && !x.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
            }
        }
    }

}
