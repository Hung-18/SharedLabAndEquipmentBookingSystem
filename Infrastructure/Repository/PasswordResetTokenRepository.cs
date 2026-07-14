using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repository
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly ApplicationDbContext _context;
        public PasswordResetTokenRepository(ApplicationDbContext context) => _context = context;

        public async Task AddAsync(PasswordResetToken token) => await _context.PasswordResetTokens.AddAsync(token);

        public async Task<PasswordResetToken?> GetByTokenAsync(string email, string token)
            => await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Email == email && t.Token == token);

        public async Task<IEnumerable<PasswordResetToken>> GetByEmailAsync(string email)
            => await _context.PasswordResetTokens.Where(t => t.Email == email).ToListAsync();

        public async Task RemoveRangeAsync(IEnumerable<PasswordResetToken> tokens)
            => _context.PasswordResetTokens.RemoveRange(tokens);

        public async Task DeleteAsync(PasswordResetToken token)
            => _context.PasswordResetTokens.Remove(token);
    }
}
