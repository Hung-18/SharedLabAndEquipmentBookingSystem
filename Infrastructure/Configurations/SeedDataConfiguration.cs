using Domain;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Configurations
{
    internal static class SeedDataConfiguration
    {
        internal static void Apply(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new
                {
                    RoleId = 1,
                    RoleName = RoleName.Admin,
                    Description = "System administrator"
                },
                new
                {
                    RoleId = 2,
                    RoleName = RoleName.LabManager,
                    Description = "Lab room manager"
                },
                new
                {
                    RoleId = 3,
                    RoleName = RoleName.Requester,
                    Description = "User who books lab rooms or equipment"
                }
            );

            modelBuilder.Entity<Department>().HasData(
                new
                {
                    DepartmentId = 1,
                    DepartmentName = "Information Technology",
                    Description = "Department of Information Technology",
                    Status = DepartmentStatus.Active
                },
                new
                {
                    DepartmentId = 2,
                    DepartmentName = "Electrical and Electronic Engineering",
                    Description = "Department of Electrical and Electronic Engineering",
                    Status = DepartmentStatus.Active
                },
                new
                {
                    DepartmentId = 3,
                    DepartmentName = "Biology",
                    Description = "Department of Biology",
                    Status = DepartmentStatus.Active
                },
                new
                {
                    DepartmentId = 4,
                    DepartmentName = "Physics",
                    Description = "Department of Physics",
                    Status = DepartmentStatus.Active
                }
            );

            modelBuilder.Entity<PriorityRule>().HasData(
                new
                {
                    PriorityRuleId = 1,
                    PurposeType = BookingPurposeType.ResearchProject,
                    PriorityLevel = 1,
                    Description = "Highest priority for research projects",
                    Status = PriorityRuleStatus.Active
                },
                new
                {
                    PriorityRuleId = 2,
                    PurposeType = BookingPurposeType.CoursePractice,
                    PriorityLevel = 2,
                    Description = "Priority for course practice sessions",
                    Status = PriorityRuleStatus.Active
                },
                new
                {
                    PriorityRuleId = 3,
                    PurposeType = BookingPurposeType.SelfStudy,
                    PriorityLevel = 3,
                    Description = "Priority for self-study or independent practice",
                    Status = PriorityRuleStatus.Active
                },
                new
                {
                    PriorityRuleId = 4,
                    PurposeType = BookingPurposeType.Other,
                    PriorityLevel = 4,
                    Description = "Other booking purposes",
                    Status = PriorityRuleStatus.Active
                }
            );
        }
    }
}
