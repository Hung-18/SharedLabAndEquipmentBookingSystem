using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Role
    {
        protected Role() { }

        public Role(RoleName roleName, string? description = null)
        {
            if (!Enum.IsDefined(typeof(RoleName), roleName))
                throw new ArgumentException("Invalid role name");

            RoleName = roleName;
            Description = description?.Trim();
        }

        public int RoleId { get; private set; }

        public RoleName RoleName { get; private set; }

        public string? Description { get; private set; }

        public ICollection<User> Users { get; private set; } = new List<User>();
    }
}
