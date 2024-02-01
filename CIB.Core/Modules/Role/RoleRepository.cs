using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Role.Dto;

namespace CIB.Core.Modules.Role
{
  public class RoleRepository : Repository<TblRole>, IRoleRepository
  {
    public RoleRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public string GetRoleName(string roleId)
    {
      return _context.TblRoles.FirstOrDefault(a => a.Id.ToString().Equals(roleId))?.RoleName;
    }

    public List<RolePermissionDto> GetUserPermissions(string roleId)
    {
     return _context.TblRoleUserAccesses.Where(a => a.RoleId != null && a.RoleId.ToLower() == roleId.ToLower())
                        .Join(_context.TblUserAccesses, ra => ra.UserAccessId, ua => ua.Id.ToString(), (ra, ua) => new RolePermissionDto { Id = ua.Id, Name = ua.Name })
                        .ToList();
    }

    public void UpdateRole(TblRole update)
    {
       _context.Update(update).Property(x=>x.Sn).IsModified = false;
    }

    public TblRole GetAuthorizer()
    {
      return _context.TblRoles.Where(ctx => ctx.RoleName.Trim().ToLower() == "").FirstOrDefault();
    }

    TblRole IRoleRepository.GetRoleById(string roleId)
    {
       return _context.TblRoles.FirstOrDefault(a => a.Id.ToString().Equals(roleId));
    }
  }
}