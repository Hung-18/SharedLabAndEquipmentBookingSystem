using Domain;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository
{
    public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token,CancellationToken cancellationToken = default)
        {
            return await Context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            await Context.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await Context.RefreshTokens
                .Where(x =>
                    x.UserId == userId
                    && x.Status == RefreshTokenStatus.Active
                    && x.RevokedAt == null
                    && x.ExpiresAt > now)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task RevokeAllByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var tokens = await Context.RefreshTokens
                .Where(x =>
                    x.UserId == userId
                    && x.Status == RefreshTokenStatus.Active
                    && x.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.Revoke();
            }
        }
    }

}
