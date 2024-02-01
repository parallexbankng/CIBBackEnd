using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.CorporateRole.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.CorporateRole.Validation
{
    public class CreateCorporateRoleValidation : AbstractValidator<CreateCorporateRoleDto>
    {
        public CreateCorporateRoleValidation()
        {
            RuleFor(p => p.RoleName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.ApprovalLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }

    public class UpdateCorporateRoleValidation : AbstractValidator<UpdateCorporateRoleDto>
    {
        
    }
}