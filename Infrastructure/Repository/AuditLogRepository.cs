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
    public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;
        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
            int userId,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = Context.AuditLogs
                .Where(x => x.UserId == userId)
                .AsQueryable();

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= to.Value);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
            string entityName,
            int entityId,
            CancellationToken cancellationToken = default)
        {
            var normalizedEntityName = entityName.Trim().ToLower();

            return await Context.AuditLogs
                .Where(x =>
                    x.EntityName.ToLower() == normalizedEntityName
                    && x.EntityId == entityId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task LogAsync(AuditLog auditLog, CancellationToken cancelation)
        {
            await Context.AuditLogs.AddAsync(auditLog, cancelation);
            await Context.SaveChangesAsync();

        }
    }

}
