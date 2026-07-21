using Domain;

namespace Application.DTOs.Users
{
    public class SetUserStatusRequest
    {
        public UserStatus Status { get; set; }
        public DateTime? RestrictionUntil { get; set; }
    }
}
