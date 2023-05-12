using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.TransactionReversalService.Entities
{
    public partial class TblCorporateRoleUserAccess
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string CorporateRoleId { get; set; }
        public string UserAccessId { get; set; }
    }
}
