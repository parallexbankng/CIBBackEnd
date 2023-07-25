using System;
using CIB.Core.Common;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto
{
    public class CreateCorporateEmployeeRequest : BaseDto
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
    public class CreateCorporateEmployeeDto : BaseDto
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
    }
    public class UpdateCorporateEmployeeRequest : BaseDto
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
    public class UpdateCorporateEmployeeDto : BaseDto
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

    public class EmployeeBulkUploadDto :BaseDto
    {
        public IFormFile files { get; set; }
    }

}