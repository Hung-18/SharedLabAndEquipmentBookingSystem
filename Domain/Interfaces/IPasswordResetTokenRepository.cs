using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task AddAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default);
        Task<PasswordResetToken?> GetByTokenHashAsync(
            string email,
            string tokenHash,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PasswordResetToken>> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);
        Task RemoveRangeAsync(
            IEnumerable<PasswordResetToken> tokens,
            CancellationToken cancellationToken = default);
    }
}
