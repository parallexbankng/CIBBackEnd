using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.Role.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.Role.Validation
{
    public class CreateRoleValidation :  AbstractValidator<CreateRoleDto>
    {
         public CreateRoleValidation()
        {

            RuleFor(p => p.RoleName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Grade)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();     
        }
    }

    public class UpdateRoleValidation :  AbstractValidator<UpdateRoleDto>
    {
         public UpdateRoleValidation()
        {

            RuleFor(p => p.RoleName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Grade)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();     
        }
    }

    public class RoleIdValidation :  AbstractValidator<UpdateRoleDto>
    {
         public RoleIdValidation()
        {
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();     
        }
    }
}