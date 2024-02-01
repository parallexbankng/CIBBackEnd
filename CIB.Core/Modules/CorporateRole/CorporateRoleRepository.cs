using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateRole
{
    public class CorporateRoleRepository : Repository<TblCorporateRole>, ICorporateRoleRepository
    {
        public CorporateRoleRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
            get { return _context as ParallexCIBContext; }
        }

        public IEnumerable<TblCorporateRole> GetAllCorporateRolesByCorporateId(Guid corporateCustomerId)
        {
            return _context.TblCorporateRoles.Where(x => x.CorporateCustomerId == corporateCustomerId || x.CorporateCustomerId == null);
        }

        public string GetCorporateRoleName(string roleId)
        {
            return _context.TblCorporateRoles.FirstOrDefault(a => a.Id.ToString().Equals(roleId))?.RoleName;
        }

        public void UpdateCorporateRole(TblCorporateRole update)
        {
            _context.Update(update).Property(x => x.Sn).IsModified = false;
        }
    }
}