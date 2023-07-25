using System;


namespace CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto
{
    public class ScheduleBeneficiaryResponse
    {
        public Guid Id { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public Guid? EmployeeId { get; set; }
        public string FullName { get; set; }
        public decimal? Amount { get; set; }
        // public int? Status { get; set; }
    }
   
}