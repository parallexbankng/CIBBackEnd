using System;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.CorporateCustomerSalary.Validation
{
    public class CreateCorporateSalaryScheduleValidation :  AbstractValidator<CreateCorporateCustomerSalaryDto>
    {    
        public CreateCorporateSalaryScheduleValidation()
        {
            RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AccountNumber)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly)
                .NotNull();
            RuleFor(p => p.Frequency.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.NumberOfBeneficairy)
                .NotNull().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.TriggerType.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .EmailAddress().WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.StartDate)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Discription.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
          
        }
    }
    public class UpdateCorporateSalaryScheduleValidation :  AbstractValidator<UpdateCorporateCustomerSalaryDto>
    {    
        public UpdateCorporateSalaryScheduleValidation()
        {
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.CorporateCustomerId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AccountNumber)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .Matches(new ReqEx().NumberOnly)
                .NotNull();
            RuleFor(p => p.Frequency.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.NumberOfBeneficairy)
                .NotNull().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.TriggerType.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .EmailAddress().WithMessage("{PropertyName} is not valid.")
                .NotNull();
            RuleFor(p => p.StartDate)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Discription.Trim())
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
           
        }
    }
}