using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Notifications
{
    public class NotificationResponse
    {
        public int NotificationId { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string NotificationType { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

}
