using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.CorporateUserRoleAccess
{
    public class CorporateUserRoleAccess:Repository<TblCorporateRoleUserAccess>, ICorporateUserRoleAccess
    {
        public CorporateUserRoleAccess(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
            get { return _context as ParallexCIBContext; }
        }

        public bool AccessesExist(string roleId, string accessName)
        {   
            if(string.IsNullOrEmpty(roleId))return false;
            var list = _context.TblCorporateRoleUserAccesses.Where(a => a.CorporateRoleId.ToLower() == roleId.ToLower())
                .Join(_context.TblUserAccesses, ra => ra.UserAccessId, ua => ua.Id.ToString(), (ra, ua) => new { ua.Name })
                .FirstOrDefault(x => x.Name.ToLower().Equals(accessName.ToLower()));
            return list != null ? true : false;
        }

    public List<TblCorporateRoleUserAccess> GetCorporatePermissions(Guid corporateRoleId)
    {
      return _context.TblCorporateRoleUserAccesses.Where(a => a.CorporateRoleId == corporateRoleId.ToString()).ToList();   
    }

    public TblCorporateRoleUserAccess GetCorporateRoleUserAccesses(string corporateRoleId, string userAccess)
    {
       return _context.TblCorporateRoleUserAccesses.Where(a => a.CorporateRoleId == corporateRoleId && a.UserAccessId.ToLower() == userAccess).FirstOrDefault();   
    }

    public List<UserAccessModel> GetCorporateUserPermissions(string corporateRoleId)
        {
           return  _context.TblCorporateRoleUserAccesses.Where(a => a.CorporateRoleId.ToLower() == corporateRoleId.ToLower())
                        .Join(_context.TblUserAccesses, ra => ra.UserAccessId, ua => ua.Id.ToString(), (ra, ua) => new UserAccessModel { Id = ua.Id, Name = ua.Name })
                        .ToList();
        }

    public bool IsCorporateAdmin(string roleId)
    {
       if (!string.IsNullOrEmpty(roleId))
        {
            var Id = Guid.Parse(roleId);
            var role = _context.TblCorporateRoles.SingleOrDefault(a => a.Id == Id);
            if (role != null)
            {
                if (role.RoleName?.ToLower()?.Trim() == "corporate admin") return true;
            }
        }
        return false;
    }
  }
}