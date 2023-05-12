using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.UserRoleAccess.Dto
{
    public class RoleAccessResponseDto
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string RoleId { get; set; }
        public string UserAccessId { get; set; }
    }
}