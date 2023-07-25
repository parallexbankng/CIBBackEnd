using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto
{
    public class CreateBeneficiaryRequestDto: BaseDto
    {
        public Guid? CorporateCustomerId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? ScheduleId { get; set; }
        public List<Beneficiary>? Beneficiaries {get;set;}
    }

    public class CreateBeneficiaryRequest: BaseDto
    {
        public string CorporateCustomerId { get; set; }
        public string ScheduleId { get; set; }
        public string Beneficiaries {get;set;}
    }

    public class Beneficiary
    {  
        public Guid? EmployeeId { get; set; }
        public decimal? Amount { get; set; }
    }

    public class UpdateBeneficiaryDto : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public List<Beneficiary>? Beneficiaries {get;set;}
    }


    public class UpdateABeneficiary: BaseUpdateDto
    {
        public string Id { get; set; }
        public string Beneficiaries {get;set;}
    }
}