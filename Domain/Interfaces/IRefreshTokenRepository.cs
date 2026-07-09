using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        Task AddRefreshTokenAsync(RefreshToken refreshToken);

        Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task RevokeAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }

}
