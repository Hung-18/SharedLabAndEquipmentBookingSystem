using System.Security.Cryptography;
using System.Text;

namespace Domain.Entities
{
    public class RefreshToken
    {
        protected RefreshToken() { }

        public RefreshToken(
            int userId,
            string rawToken,
            DateTime expiresAt)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException("Token không được để trống");

            if (expiresAt <= DateTime.UtcNow)
                throw new ArgumentException(
                    "Thời gian hết hạn phải lớn hơn hiện tại");

            UserId = userId;
            TokenHash = ComputeHash(rawToken);
            ExpiresAt = expiresAt;
            CreatedAt = DateTime.UtcNow;
            Status = RefreshTokenStatus.Active;
        }

        public int RefreshTokenId { get; private set; }

        public int UserId { get; private set; }

        /// <summary>
        /// SHA-256 hash of the refresh token. The raw token is returned to the
        /// client once and is never persisted.
        /// </summary>
        public string TokenHash { get; private set; } = string.Empty;

        public DateTime ExpiresAt { get; private set; }

        public DateTime? RevokedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public RefreshTokenStatus Status { get; private set; } =
            RefreshTokenStatus.Active;

        public User? User { get; private set; }

        public bool IsActive =>
            Status == RefreshTokenStatus.Active
            && RevokedAt == null
            && ExpiresAt > DateTime.UtcNow;

        public static string ComputeHash(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                throw new ArgumentException(
                    "Token không được để trống.",
                    nameof(rawToken));

            byte[] tokenBytes =
                Encoding.UTF8.GetBytes(rawToken.Trim());

            return Convert.ToHexString(
                SHA256.HashData(tokenBytes));
        }

        public void Revoke()
        {
            RevokedAt = DateTime.UtcNow;
            Status = RefreshTokenStatus.Revoked;
        }

        public void MarkExpired()
        {
            Status = RefreshTokenStatus.Expired;
        }
    }
}
