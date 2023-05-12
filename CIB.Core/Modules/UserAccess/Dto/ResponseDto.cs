using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.UserAccess.Dto
{
    public class UserAccessResponseDto
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool? IsCorporate { get; set; }
        public string Description { get; set; }
    }
}