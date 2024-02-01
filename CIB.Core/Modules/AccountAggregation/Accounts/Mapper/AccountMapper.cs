using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Aggregations.Dto;
namespace CIB.Core.Modules.AccountAggregation.Accounts.Mapper;

public class AccountAgggregationMapper : Profile
{
	public AccountAgggregationMapper()
	{
		CreateMap<TblTempCorporateAccountAggregation, CreateAggregateCorporateCustomerModel>().ReverseMap();
		CreateMap<TblTempCorporateAccountAggregation, TblCorporateAccountAggregation>().ReverseMap();


	}
}

