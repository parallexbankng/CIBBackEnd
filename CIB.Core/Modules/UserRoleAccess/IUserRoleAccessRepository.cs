using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.UserRoleAccess.Dto;

namespace CIB.Core.Modules.UserRoleAccess
{
  public interface IUserRoleAccessRepository :IRepository<TblRoleUserAccess>
  {
    bool AccessesExist(string roleId, string accessName);  
    List<TblRoleUserAccess> GetRoleUserAccessesByRoleID(string RoleId);
    TblRoleUserAccess GetRoleUserAccessesByRoleID(string RoleId,string userAccessId );
    bool IsSuperAdmin(string roleId);
    bool IsSuperAdminAuthorizer(string roleId);
    bool IsSuperAdminMaker(string roleId);
  }
}