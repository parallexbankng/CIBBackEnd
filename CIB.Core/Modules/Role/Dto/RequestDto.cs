using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.Role.Dto
{
    public class CreateRoleDto : BaseDto
    {
        public string RoleName { get; set; }
        public int? Grade { get; set; }
    }

    public class CreateRole : BaseDto
    {
        public string RoleName { get; set; }
        public string Grade { get; set; }
    }
    public class UpdateRoleDto : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public int? Grade { get; set; }
    }
    public class UpdateRole :BaseUpdateDto
    {
        public string Id { get; set; }
        public string RoleName { get; set; }
        public string Grade { get; set; }
    }

    public class RolePermissionDto 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsCorporate { get; set; }
    }
    public class RoleIdDto 
    {
        public Guid Id { get; set; }
    }
}