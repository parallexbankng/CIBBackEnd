using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.UserAccess.Dto
{
    public class CreateRequestDto: BaseDto
    {
        public string Name { get; set; }
        public bool IsCorporate { get; set; }
    }

     public class CreateRequest: BaseUpdateDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IsCorporate { get; set; }
    }

    public class SetPermissionCreateRequestDto : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsCorporate { get; set; }
    }

    public class UpdateRequestDto : CreateRequestDto { }

    public class AddRoleAccessRequestDto : BaseUpdateDto
    {
        public string RoleId { get; set; }
        public List<string> AccessIds { get; set; }
    }
}