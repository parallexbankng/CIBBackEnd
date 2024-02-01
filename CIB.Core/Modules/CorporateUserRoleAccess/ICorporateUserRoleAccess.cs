using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.CorporateUserRoleAccess
{
  public interface ICorporateUserRoleAccess : IRepository<TblCorporateRoleUserAccess>
  {
    bool AccessesExist(string roleId, string accessName);
    List<UserAccessModel> GetCorporateUserPermissions(string corporateRoleId);
    List<TblCorporateRoleUserAccess> GetCorporatePermissions(Guid corporateRoleId);
    TblCorporateRoleUserAccess GetCorporateRoleUserAccesses(string corporateRoleId, string userAccess);
    bool IsCorporateAdmin(string roleId);
    List<TblUserAccess> GetPermissions();
  }
}