using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Notifications
{
    public class UnreadNotificationCountResponse
    {
        public int UserId { get; set; }

        public int UnreadCount { get; set; }
    }

}
