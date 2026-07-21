using Domain;

namespace Application.DTOs.Users
{
    public class UserPenaltyResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int PenaltyPoints { get; set; }
        public UserStatus Status { get; set; }
        public DateTime? RestrictionUntil { get; set; }
    }
}
