using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblRoleUserAccess
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string RoleId { get; set; }
        public string UserAccessId { get; set; }
    }
}
