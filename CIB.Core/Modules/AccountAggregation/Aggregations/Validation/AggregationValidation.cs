using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.AccountAggregation.Aggregations.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;
using CIB.Core.Utils;
using FluentValidation;

namespace CIB.Core.Modules.AccountAggregation.Aggregations.Validation;

public class AggregationValidation
{

}
public class CreateCorporateAccountAggregationValidation : AbstractValidator<CreateAggregateCorporateCustomerModel>
{
	public CreateCorporateAccountAggregationValidation()
	{
		RuleFor(p => p.CorporateCustomerId)
				.NotEmpty().WithMessage("{PropertyName} is required.")
				.NotNull();
		RuleFor(p => p.CustomerId)
				.NotEmpty().WithMessage("{PropertyName} is required.")
				.NotNull();
		RuleFor(p => p.DefaultAccountNumber.Trim())
				.NotEmpty().WithMessage("{PropertyName} is required.")
				.Matches(new ReqEx().NumberOnly).WithMessage("{PropertyName} is not valid.")
				.NotNull();
		RuleFor(p => p.DefaultAccountName.Trim())
				.NotEmpty().WithMessage("{PropertyName} is required.")
				.NotNull();
	}
}

