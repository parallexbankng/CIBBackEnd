using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.Workflow.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.Workflow.Validation
{
  public class CreateWorkFlowValidation : AbstractValidator<CreateWorkflowDto>
  {
    public CreateWorkFlowValidation()
    {
        RuleFor(p => p.Name.Trim())
          .NotEmpty().WithMessage("{PropertyName} is required.")
          .NotNull()
          .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
        RuleFor(p => p.Description)
          .NotEmpty().WithMessage("{PropertyName} is required.")
          .NotNull();
        RuleFor(p => p.NoOfAuthorizers)
          .NotEmpty().WithMessage("{PropertyName} is required.")
          .NotNull();
    }
  }

  public class CreateCorporateWorkFlowValidation : AbstractValidator<CreateCorporateWorkflowDto>
  {
    public CreateCorporateWorkFlowValidation()
    {
      RuleFor(p => p.Name.Trim())
        .NotEmpty().WithMessage("{PropertyName} is required.")
        .NotNull()
        .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
      RuleFor(p => p.Description)
        .NotEmpty().WithMessage("{PropertyName} is required.")
        .NotNull()
        .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
      RuleFor(p => p.NoOfAuthorizers)
        .NotEmpty().WithMessage("{PropertyName} is required.")
        .NotNull()
        .NotEqual(0).WithMessage("{PropertyName} Can not be zero.");
    }
  }

public class UpdateWorkFlowValidation: AbstractValidator<UpdateWorkflowDto>
    {
        public UpdateWorkFlowValidation(){
            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.Id)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
            RuleFor(p => p.Description)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull()
                .Matches(new ReqEx().AlphaNumeric).WithMessage("{PropertyName} is not valid.");
            RuleFor(p => p.NoOfAuthorizers)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .NotNull();
        }
    }

}