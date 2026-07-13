using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class PasswordResetToken
    {
        public int TokenId { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
