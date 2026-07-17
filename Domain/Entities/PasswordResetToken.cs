namespace Domain.Entities
{
    public class PasswordResetToken
    {
        protected PasswordResetToken() { }

        public PasswordResetToken(
            int userId,
            string email,
            string tokenHash,
            DateTime expiryDate)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.");
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("Token hash không được để trống.");
            if (expiryDate <= DateTime.UtcNow)
                throw new ArgumentException("Thời hạn token phải ở tương lai.");

            UserId = userId;
            Email = email.Trim().ToLowerInvariant();
            Token = tokenHash;
            ExpiryDate = expiryDate;
        }

        public int TokenId { get; private set; }
        public string Email { get; private set; } = string.Empty;
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiryDate { get; private set; }
        public bool IsExpired(DateTime now) => ExpiryDate <= now;
        public int UserId { get; private set; }
        public User User { get; private set; } = null!;
    }
}
