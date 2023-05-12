using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Validation
{
    public class CreateCorporateEmployeeValidation :  AbstractValidator<CreateCorporateEmployeeDto>
    {    
        public CreateCorporateEmployeeValidation()
        {
            RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.LastName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.StaffId)
                .NotNull().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Department.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .EmailAddress().WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.AccountName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AccountNumber.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.BankCode)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.SalaryAmount)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }
    public class UpdateCorporateEmployeeValidation :  AbstractValidator<UpdateCorporateEmployeeDto>
    {    
        public UpdateCorporateEmployeeValidation()
        {
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
           RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.LastName.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.StaffId)
                .NotNull().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Department.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .EmailAddress().WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.AccountName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AccountNumber.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.BankCode)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.SalaryAmount)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }
}