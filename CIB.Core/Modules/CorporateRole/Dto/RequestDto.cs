using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.CorporateRole.Dto
{
    public class CreateCorporateRoleDto: BaseDto
    {
        public string RoleName { get; set; }
        public decimal? ApprovalLimit { get; set; }
        public Guid? CorporateCustomerId { get; set; }
    }

    public class CreateCorporateRole: BaseDto
    {
        public string RoleName { get; set; }
        public string ApprovalLimit { get; set; }
        public string CorporateCustomerId { get; set; }
    }

    public class UpdateCorporateRoleDto:BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string RoleName { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public decimal? ApprovalLimit { get; set; }
    }

    public class UpdateCorporateRole:BaseUpdateDto
    {
        public string Id { get; set; }
        public string RoleName { get; set; }
        public string CorporateCustomerId { get; set; }
        public string ApprovalLimit { get; set; }
    }
}