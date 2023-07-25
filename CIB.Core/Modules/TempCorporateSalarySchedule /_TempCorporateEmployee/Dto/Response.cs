using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee.Dto
{
    public class TempCorporateEmployeeResponse
    {
        public Guid Id {get;set;}
        public Guid? CorporateCustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StaffId { get; set; }
        public string Department { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public decimal? SalaryAmount { get; set; }
        public string GradeLevel { get; set; }
        public string Description { get; set; }
        public int? Status { get; set; }
    }
    public class CorporateEmployeeDuplicateStatus 
    {
        public string Message { get; set; }
        public bool IsDuplicate { get; set; }
    }
}