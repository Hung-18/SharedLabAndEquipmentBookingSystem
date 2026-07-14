

namespace Domain.Entities
{
    public class Notification
    {
        protected Notification()
        {
        }

        public Notification(
            int userId,
            string title,
            string message,
            NotificationType notificationType)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(
                    "UserId phải lớn hơn 0.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Tiêu đề thông báo không được để trống.");
            }

            if (title.Trim().Length > 150)
            {
                throw new ArgumentException(
                    "Tiêu đề thông báo không được vượt quá 150 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "Nội dung thông báo không được để trống.");
            }

            if (!Enum.IsDefined(
                    typeof(NotificationType),
                    notificationType))
            {
                throw new ArgumentException(
                    "Loại thông báo không hợp lệ.");
            }

            UserId = userId;
            Title = title.Trim();
            Message = message.Trim();
            NotificationType = notificationType;
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
        }

        public int NotificationId { get; private set; }

        public int UserId { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string Message { get; private set; } = string.Empty;

        public NotificationType NotificationType { get; private set; }

        public bool IsRead { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public User? User { get; private set; }

        public void MarkAsRead()
        {
            if (IsRead)
            {
                return;
            }

            IsRead = true;
        }
    }

}