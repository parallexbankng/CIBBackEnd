using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.TempCorporateSalarySchedule.Dto
{
    public class TempCreateCorporateCustomerSalaryDto :BaseDto
    {
        public string CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string ScheduleCategory {get;set;}
        public string TriggerType { get; set; }
        public string StartDate { get; set; }
        public string Discription { get; set; }
        public string CreatedBy { get; set; }
        public string WorkFlowId { get; set; }
    }

    public class TempCreateCorporateCustomerSalaryRequestDto : BaseDto
    {
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public int ScheduleCategory {get;set;}
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? WorkFlowId { get; set; }
    }

    public class TempUpdateCorporateCustomerSalaryDto : BaseDto
    {
        public Guid Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public int ScheduleCategory {get;set;}
        public string TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public Guid? UpdatedAt { get; set; }
        public Guid? WorkFlowId { get; set; }
    }
    public class TempUpdateCorporateCustomerSalaryRequestDto
    {
        public Guid Id { get; set; }
        public string CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public int ScheduleCategory {get;set;}
        public string TriggerType { get; set; }
        public string StartDate { get; set; }
        public string Discription { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string WorkFlowId { get; set; }
    }
}