using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.WorkflowHierarchy.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.WorkflowHierarchy.Validation
{
    public class CreateWorkflowHierachyValidation : AbstractValidator<CreateWorkflowHierarchyDto>
    {
        public CreateWorkflowHierachyValidation(){
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.RoleId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.RoleName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.ApproverId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.ApproverName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AuthorizationLevel)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
             RuleFor(p => p.WorkflowId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();  
        }
    }

    public class UpdateWorkflowHierachyValidation: AbstractValidator<UpdateWorkflowHierarchyDto>
    {
        public UpdateWorkflowHierachyValidation(){
            RuleFor(p => p.RoleId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.RoleName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphabetOnly).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.ApproverId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.ApproverName)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AuthorizationLevel)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
             RuleFor(p => p.WorkflowId)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.AccountLimit)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
    }
    }
}