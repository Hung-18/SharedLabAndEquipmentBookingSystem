
namespace Domain.Entities
{
    public class AuditLog
    {
        protected AuditLog() { }

        public AuditLog(
            int userId,
            AuditActionType actionType,
            string entityName,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string? ipAddress = null)
        {
            if (userId <= 0)
                throw new ArgumentException("UserId phải lớn hơn 0");

            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("EntityName không được để trống");

            if (entityId <= 0)
                throw new ArgumentException("EntityId phải lớn hơn 0");

            UserId = userId;
            ActionType = actionType;
            EntityName = entityName.Trim();
            EntityId = entityId;
            OldValue = oldValue;
            NewValue = newValue;
            IpAddress = ipAddress?.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        public int AuditLogId { get; private set; }

        public int UserId { get; private set; }

        public AuditActionType ActionType { get; private set; }

        public string EntityName { get; private set; } = string.Empty;

        public int EntityId { get; private set; }

        public string? OldValue { get; private set; }

        public string? NewValue { get; private set; }

        public string? IpAddress { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public User? User { get; private set; }
    }
}