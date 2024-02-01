using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.CorporateRole.Dto
{
    public class CorporateRoleResponseDto
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public int Status { get; set; }
    }

    public class CorporateRoleResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    
}