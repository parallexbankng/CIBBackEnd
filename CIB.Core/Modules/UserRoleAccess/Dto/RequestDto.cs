using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.UserRoleAccess.Dto
{
    public class AddRoleAccessRequestDto : BaseDto
    {
        public string RoleId { get; set; }
        public string UserAccessId { get; set; }
    }
}