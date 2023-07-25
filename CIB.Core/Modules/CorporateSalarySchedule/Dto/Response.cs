using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.CorporateSalarySchedule.Dto
{
    public class CorporateCustomerSalaryResponseDto
    {
        public Guid Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public DateTime? DateCreated { get; set; }
        public string ApproverUserName { get; set; }
        public int? Status { get; set; }
        public string AccountName { get; set; }
        public string Currency { get; set; }
    }
    public class SalaryScheduleDuplicateStatus 
    {
        public string Message { get; set; }
        public bool IsDuplicate { get; set; }
    }
}