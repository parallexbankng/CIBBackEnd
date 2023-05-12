using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Role.Dto;

namespace CIB.Core.Modules.Role
{
    public interface IRoleRepository : IRepository<TblRole>
    {
      string GetRoleName(string roleId);
      TblRole GetAuthorizer();
      TblRole GetRoleById(string roleId);
      void UpdateRole(TblRole update);
      List<RolePermissionDto> GetUserPermissions(string roleId);
  }
}