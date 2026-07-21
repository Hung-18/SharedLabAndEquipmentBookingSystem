using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Booking
{
    public class BookingDetailResponse
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int? PriorityRuleId { get; set; }
        public int? PriorityLevel { get; set; }
        public int? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public string PurposeType { get; set; } = string.Empty;
        public string PurposeDescription { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BookingItemResponse> Items { get; set; } = new();
    }

}
