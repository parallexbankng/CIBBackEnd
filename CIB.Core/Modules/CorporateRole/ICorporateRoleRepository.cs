using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateRole
{
    public interface ICorporateRoleRepository : IRepository<TblCorporateRole>
    {
        string GetCorporateRoleName(string roleId);
        IEnumerable<TblCorporateRole> GetAllCorporateRolesByCorporateId(Guid corporateCustomerId);
        void UpdateCorporateRole(TblCorporateRole update);
    }
}