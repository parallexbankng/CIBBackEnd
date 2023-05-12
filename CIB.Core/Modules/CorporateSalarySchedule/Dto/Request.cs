using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.CorporateSalarySchedule.Dto
{
    public class CreateCorporateCustomerSalaryDtoRequest :BaseDto
    {
        public string CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string IsSalary {get;set;}
        public string TriggerType { get; set; }
        public string StartDate { get; set; }
        public string Discription { get; set; }
        public string CreatedBy { get; set; }
        public string WorkFlowId { get; set; }
    }

    public class CreateCorporateCustomerSalaryDto : BaseDto
    {
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public string TriggerType { get; set; }
        public bool IsSalary {get;set;}
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public int ScheduleCategory {get;set;}
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? WorkFlowId { get; set; }
    }

    public class UpdateCorporateCustomerSalaryDtoRequest : BaseDto
    {
        public string Id { get; set; }
        public string CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public int ScheduleCategory {get;set;}
        public string TriggerType { get; set; }
        public string StartDate { get; set; }
        public string IsSalary {get;set;}
        public string Discription { get; set; }
        public string UpdatedAt { get; set; }
        public string WorkFlowId { get; set; }
    }
    public class UpdateCorporateCustomerSalaryDto:BaseDto
    {
        public Guid Id { get; set; }
        public Guid CorporateCustomerId { get; set; }
        public string AccountNumber { get; set; }
        public string Frequency { get; set; }
        public string NumberOfBeneficairy { get; set; }
        public bool IsSalary {get;set;}
        public string TriggerType { get; set; }
        public DateTime? StartDate { get; set; }
        public string Discription { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid WorkFlowId { get; set; }
    }
}