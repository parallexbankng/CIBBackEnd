using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.UserAccess
{
    public interface IUserAccessRepository:IRepository<TblUserAccess>
    {
        TblUserAccess GetUserAccessByCode(string code);
        IEnumerable<TblUserAccess> GetAllCorporateUserAccesses();
        List<UserAccessModel> GetUserPermissions(string roleId);

        void UpdateCorporateUserPermissions(TblUserAccess roleId);
  }
}