using System;
using System.Collections.Generic;

namespace Permission_Based_Authorization.Contexts.UserManagement.Models;

public partial class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
