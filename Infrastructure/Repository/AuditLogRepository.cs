using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository
{
    public class AuditLogRepository
       : BaseRepository<AuditLog>,
         IAuditLogRepository
    {
        public AuditLogRepository(
            ApplicationDbContext context)
            : base(context)
        {
        }

        public override async Task<AuditLog?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await Context.AuditLogs
                .AsNoTracking()
                .Include(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.AuditLogId == id,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
            int userId,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = Context.AuditLogs
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.UserId == userId)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt <= to.Value);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.AuditLogId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
            string entityName,
            int entityId,
            CancellationToken cancellationToken = default)
        {
            string normalizedEntityName =
                entityName.Trim().ToLower();

            return await Context.AuditLogs
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x =>
                    x.EntityName.ToLower()
                        == normalizedEntityName
                    && x.EntityId == entityId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.AuditLogId)
                .ToListAsync(cancellationToken);
        }

        public async Task<(
            IReadOnlyList<AuditLog> Items,
            int TotalCount)> SearchAsync(
                int? userId,
                AuditActionType? actionType,
                string? entityName,
                int? entityId,
                DateTime? from,
                DateTime? to,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            var query = Context.AuditLogs
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(
                    x => x.UserId == userId.Value);
            }

            if (actionType.HasValue)
            {
                query = query.Where(
                    x => x.ActionType == actionType.Value);
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                string normalizedEntityName =
                    entityName.Trim().ToLower();

                query = query.Where(
                    x => x.EntityName.ToLower()
                        == normalizedEntityName);
            }

            if (entityId.HasValue)
            {
                query = query.Where(
                    x => x.EntityId == entityId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(
                    x => x.CreatedAt <= to.Value);
            }

            int totalCount =
                await query.CountAsync(
                    cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.AuditLogId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }


}
