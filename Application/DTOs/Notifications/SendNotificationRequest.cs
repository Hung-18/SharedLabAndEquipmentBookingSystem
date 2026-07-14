using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Notifications
{
    public class SendNotificationRequest
    {
        // Chỉ Admin được gửi thủ công qua API này.
        public int ActorUserId { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType NotificationType { get; set; }
    }

}
