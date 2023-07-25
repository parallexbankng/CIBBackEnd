using System;
using CIB.Core.Common;

namespace CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee.Dto
{
    public class TempCreateCorporateEmployeeRequest : BaseDto
    {
        public string CorporateCustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StaffId { get; set; }
        public string Department { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string SalaryAmount { get; set; }
        public string GradeLevel { get; set; }
        public string Description { get; set; }
    }
    public class TempCreateCorporateEmployeeDto : BaseDto
    {
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
        public string BankName { get; set; }
    }
    public class TempUpdateCorporateEmployeeRequest : BaseDto
    {
        public string Id { get; set; }
        public string CorporateCustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StaffId { get; set; }
        public string Department { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string SalaryAmount { get; set; }
        public string GradeLevel { get; set; }
        public string Description { get; set; }
    }
    public class TempUpdateCorporateEmployeeDto : BaseDto
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
        public DateTime? DateCreated { get; set; }
        public Guid? CreatedBy { get; set; }
    }
}