using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblCorporateSalaryScheduleBeneficiary
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? EmployeeId { get; set; }
        public decimal? Amount { get; set; }
        public int? Status { get; set; }
        public DateTime? DateCreated { get; set; }
        public Guid? ScheduleId { get; set; }
    }
}
