using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.CorporateSalarySchedule.Dto
{
    public class TempCorporateCustomerSalaryResponseDto
    {
        public Guid Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public byte? IsSalary { get; set; }
        public string AccountName { get; set; }
        public string Currency { get; set; }
    }
}