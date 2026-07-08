using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BookingItems_LabId_EquipmentId",
                table: "BookingItems");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Waitlists_OneResourceOnly",
                table: "Waitlists",
                sql: "\r\n(\r\n    ([LabId] IS NOT NULL AND [EquipmentId] IS NULL)\r\n    OR\r\n    ([LabId] IS NULL AND [EquipmentId] IS NOT NULL)\r\n)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Waitlists_QueuePosition",
                table: "Waitlists",
                sql: "[QueuePosition] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Waitlists_RequestedStart_RequestedEnd",
                table: "Waitlists",
                sql: "[RequestedStart] < [RequestedEnd]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Waitlists_Status",
                table: "Waitlists",
                sql: "[Status] IN ('Waiting', 'Notified', 'Booked', 'Cancelled', 'Expired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Violations_PenaltyPointsAdded",
                table: "Violations",
                sql: "[PenaltyPointsAdded] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Violations_Status",
                table: "Violations",
                sql: "[Status] IN ('Active', 'Resolved', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Violations_ViolationType",
                table: "Violations",
                sql: "[ViolationType] IN ('NoShow', 'LateCheckout', 'DamageEquipment', 'MisuseEquipment', 'UnauthorizedUse')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_PenaltyPoints",
                table: "Users",
                sql: "[PenaltyPoints] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Status",
                table: "Users",
                sql: "[Status] IN ('Active', 'Inactive', 'Restricted', 'Locked')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageLogs_Checkin_Checkout",
                table: "UsageLogs",
                sql: "[ActualCheckout] IS NULL OR [ActualCheckin] <= [ActualCheckout]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsageLogs_IncidentStatus",
                table: "UsageLogs",
                sql: "[IncidentStatus] IN ('None', 'DamageReported', 'LateCheckout', 'MissingEquipment', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles",
                sql: "[RoleName] IN ('Admin', 'LabManager', 'Requester')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshTokens_ExpiresAt_CreatedAt",
                table: "RefreshTokens",
                sql: "[ExpiresAt] > [CreatedAt]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshTokens_Status",
                table: "RefreshTokens",
                sql: "[Status] IN ('Active', 'Revoked', 'Expired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriorityRules_PriorityLevel",
                table: "PriorityRules",
                sql: "[PriorityLevel] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriorityRules_PurposeType",
                table: "PriorityRules",
                sql: "[PurposeType] IN ('ResearchProject', 'CoursePractice', 'SelfStudy', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriorityRules_Status",
                table: "PriorityRules",
                sql: "[Status] IN ('Active', 'Inactive')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notifications_NotificationType",
                table: "Notifications",
                sql: "[NotificationType] IN ('BookingApproved', 'BookingRejected', 'BookingReminder', 'WaitlistAvailable', 'Maintenance', 'Violation', 'System')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_MaintenanceCost",
                table: "Maintenances",
                sql: "[MaintenanceCost] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_OneResourceOnly",
                table: "Maintenances",
                sql: "\r\n(\r\n    ([LabId] IS NOT NULL AND [EquipmentId] IS NULL)\r\n    OR\r\n    ([LabId] IS NULL AND [EquipmentId] IS NOT NULL)\r\n)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_StartTime_EndTime",
                table: "Maintenances",
                sql: "[StartTime] < [EndTime]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Maintenances_Status",
                table: "Maintenances",
                sql: "[Status] IN ('Scheduled', 'InProgress', 'Completed', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LabRooms_Capacity",
                table: "LabRooms",
                sql: "[Capacity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LabRooms_Status",
                table: "LabRooms",
                sql: "[Status] IN ('Available', 'Unavailable', 'Maintenance', 'Inactive')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_Status",
                table: "Equipments",
                sql: "[Status] IN ('Available', 'InUse', 'Maintenance', 'Broken', 'Retired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_Status",
                table: "Departments",
                sql: "[Status] IN ('Active', 'Inactive')");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StartTime_EndTime",
                table: "Bookings",
                columns: new[] { "StartTime", "EndTime" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_PurposeType",
                table: "Bookings",
                sql: "[PurposeType] IN ('ResearchProject', 'CoursePractice', 'SelfStudy', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_StartTime_EndTime",
                table: "Bookings",
                sql: "[StartTime] < [EndTime]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_Status",
                table: "Bookings",
                sql: "[Status] IN ('Pending', 'Approved', 'Rejected', 'Cancelled', 'Completed', 'NoShow')");

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_LabId",
                table: "BookingItems",
                column: "LabId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingItems_OneResourceOnly",
                table: "BookingItems",
                sql: "\r\n(\r\n    ([ResourceType] IN ('LabRoom', 'Lab') AND [LabId] IS NOT NULL AND [EquipmentId] IS NULL)\r\n    OR\r\n    ([ResourceType] = 'Equipment' AND [LabId] IS NULL AND [EquipmentId] IS NOT NULL)\r\n)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingItems_ResourceType",
                table: "BookingItems",
                sql: "[ResourceType] IN ('LabRoom', 'Equipment')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditLogs_ActionType",
                table: "AuditLogs",
                sql: "[ActionType] IN ('Create', 'Update', 'Delete', 'Login', 'Logout', 'ApproveBooking', 'RejectBooking', 'CheckIn', 'CheckOut')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Waitlists_OneResourceOnly",
                table: "Waitlists");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Waitlists_QueuePosition",
                table: "Waitlists");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Waitlists_RequestedStart_RequestedEnd",
                table: "Waitlists");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Waitlists_Status",
                table: "Waitlists");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Violations_PenaltyPointsAdded",
                table: "Violations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Violations_Status",
                table: "Violations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Violations_ViolationType",
                table: "Violations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_PenaltyPoints",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Status",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageLogs_Checkin_Checkout",
                table: "UsageLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UsageLogs_IncidentStatus",
                table: "UsageLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Roles_RoleName",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshTokens_ExpiresAt_CreatedAt",
                table: "RefreshTokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshTokens_Status",
                table: "RefreshTokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriorityRules_PriorityLevel",
                table: "PriorityRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriorityRules_PurposeType",
                table: "PriorityRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriorityRules_Status",
                table: "PriorityRules");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notifications_NotificationType",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_MaintenanceCost",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_OneResourceOnly",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_StartTime_EndTime",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Maintenances_Status",
                table: "Maintenances");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LabRooms_Capacity",
                table: "LabRooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LabRooms_Status",
                table: "LabRooms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_Status",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_Status",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_StartTime_EndTime",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_PurposeType",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_StartTime_EndTime",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BookingItems_LabId",
                table: "BookingItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingItems_OneResourceOnly",
                table: "BookingItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingItems_ResourceType",
                table: "BookingItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditLogs_ActionType",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_BookingItems_LabId_EquipmentId",
                table: "BookingItems",
                columns: new[] { "LabId", "EquipmentId" });
        }
    }
}
