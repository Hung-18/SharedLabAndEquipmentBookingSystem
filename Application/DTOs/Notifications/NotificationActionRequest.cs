namespace Application.DTOs.Notifications
{
    // Body rỗng dùng để giữ tương thích nếu frontend vẫn gửi {}.
    // Actor luôn được lấy từ JWT, không nhận UserId từ client.
    public sealed class NotificationActionRequest
    {
    }
}
