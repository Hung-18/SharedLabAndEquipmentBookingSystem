using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly ApplicationDbContext _context;
        public PasswordResetTokenRepository(ApplicationDbContext context) => _context = context;

        public async Task AddAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default) =>
            await _context.PasswordResetTokens.AddAsync(token, cancellationToken);

        public async Task<PasswordResetToken?> GetByTokenHashAsync(
            string email,
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.PasswordResetTokens.FirstOrDefaultAsync(
                x => x.Email == normalizedEmail && x.Token == tokenHash,
                cancellationToken);
        }

        public async Task<IReadOnlyList<PasswordResetToken>> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            return await _context.PasswordResetTokens
                .Where(x => x.Email == normalizedEmail)
                .ToListAsync(cancellationToken);
        }

        public Task RemoveRangeAsync(
            IEnumerable<PasswordResetToken> tokens,
            CancellationToken cancellationToken = default)
        {
            _context.PasswordResetTokens.RemoveRange(tokens);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default)
        {
            _context.PasswordResetTokens.Remove(token);
            return Task.CompletedTask;
        }
    }
}
