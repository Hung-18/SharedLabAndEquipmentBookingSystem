using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Violations
{
    public class CreateViolationRequest
    {
        // Người dùng bị ghi nhận vi phạm.
        public int UserId { get; set; }

        public int BookingId { get; set; }

        public ViolationType ViolationType { get; set; }
    }


}
