
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using CIB.Core.Modules.AccountAggregation.Aggregations.Dto;

namespace CIB.Core.Modules.AccountAggregation.Aggregations.Mapper;

public class CreateCorporateAccountAggregationMapper : Profile
{
	public CreateCorporateAccountAggregationMapper()
	{
		CreateMap<TblTempCorporateAccountAggregation, CreateAggregateCorporateCustomerModel>().ReverseMap();
		CreateMap<TblTempCorporateAccountAggregation, TblTempCorporateAccountAggregation>().ReverseMap();
		CreateMap<TblTempAggregatedAccount, TblAggregatedAccount>().ReverseMap();
		CreateMap<AggregatedAccountsResponseDto, TblAggregatedAccount>().ReverseMap();
		CreateMap<TblAggregatedAccount, AggregatedAccountsResponseDto>().ReverseMap();
	}
}
