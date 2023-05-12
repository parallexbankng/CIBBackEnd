using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.UserAccess
{
  public class UserAccessRepository : Repository<TblUserAccess>, IUserAccessRepository
  {
    public UserAccessRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
        get { return _context as ParallexCIBContext; }
    }
    public IEnumerable<TblUserAccess> GetAllCorporateUserAccesses()
    {
      return _context.TblUserAccesses.Where(x => x.IsCorporate == true);   
    }

    public TblUserAccess GetUserAccessByCode(string code)
    {
      throw new NotImplementedException();
    }

    public List<UserAccessModel> GetUserPermissions(string roleId)
    {
        var permissions = _context.TblRoleUserAccesses.Where(a => a.RoleId != null && a.RoleId.ToLower() == roleId.ToLower())
                        .Join(_context.TblUserAccesses, ra => ra.UserAccessId, ua => ua.Id.ToString(), (ra, ua) => new UserAccessModel { Id = ua.Id, Name = ua.Name })
                        .ToList();
        return permissions;
    }

    public void UpdateCorporateUserPermissions(TblUserAccess update)
    {
      _context.Update(update).Property(x=>x.Sn).IsModified = false;
    }
  }
}