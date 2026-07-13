using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task AddAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetByTokenAsync(string email, string token);
        Task DeleteAsync(PasswordResetToken token);
        Task<IEnumerable<PasswordResetToken>> GetByEmailAsync(string email);
        Task RemoveRangeAsync(IEnumerable<PasswordResetToken> tokens);
    }
}
