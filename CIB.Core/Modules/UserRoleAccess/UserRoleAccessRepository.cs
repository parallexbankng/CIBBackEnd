using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.UserRoleAccess.Dto;

namespace CIB.Core.Modules.UserRoleAccess
{
    public class UserRoleAccessRepository :Repository<TblRoleUserAccess>, IUserRoleAccessRepository
    {
        public UserRoleAccessRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
            get { return _context as ParallexCIBContext; }
        }

        public bool AccessesExist(string roleId, string accessName)
        {
            var list = _context.TblRoleUserAccesses.Where(a => a.RoleId != null && a.RoleId.ToLower() == roleId.ToLower())
                .Join(_context.TblUserAccesses, ra => ra.UserAccessId, ua => ua.Id.ToString(), (ra, ua) => new { ua.Name })
                .FirstOrDefault(x => x.Name.ToLower().Equals(accessName.ToLower()));
            return list != null ? true : false;
        }

        
        public List<TblRoleUserAccess> GetRoleUserAccessesByRoleID(string RoleId)
        {
            return _context.TblRoleUserAccesses.Where(a => a.RoleId != null && a.RoleId == RoleId).ToList();
        }

        public TblRoleUserAccess GetRoleUserAccessesByRoleID(string RoleId, string userAccessId)
        {
            return _context.TblRoleUserAccesses.Where(a => a.RoleId == RoleId && a.UserAccessId == userAccessId).FirstOrDefault();
        }


        public bool IsSuperAdmin(string roleId)
        {
           if (!string.IsNullOrEmpty(roleId))
            {
                var Id = Guid.Parse(roleId);
                var role = _context.TblRoles.SingleOrDefault(a => a.Id == Id);
                if (role != null)
                {
                    if (role.RoleName?.ToLower()?.Trim() == "super admin") return true;
                }
            }
            return false;
        }

        public bool IsSuperAdminAuthorizer(string roleId)
        {
            if (!string.IsNullOrEmpty(roleId))
            {
                var Id = Guid.Parse(roleId);
                var role = _context.TblRoles.SingleOrDefault(a => a.Id == Id);
                if (role != null)
                {
                    if (role.RoleName?.ToLower()?.Trim() == "super admin authorizer") return true;
                }
            }
            return false;
        }

        public bool IsSuperAdminMaker(string roleId)
        {
            if (!string.IsNullOrEmpty(roleId))
            {
                var Id = Guid.Parse(roleId);
                var role = _context.TblRoles.SingleOrDefault(a => a.Id == Id);
                if (role != null)
                {
                    if (role.RoleName?.ToLower()?.Trim() == "super admin maker") return true;
                }
            }
            return false;
        }
  }
}