using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Modules.BankAdminProfile.Mapper
{
	public class BankAdminProfileMapper : Profile
	{
		public BankAdminProfileMapper()
		{
			CreateMap<CreateBankAdminProfileDTO, TblBankProfile>();
			CreateMap<TblBankProfile, BankAdminProfileResponse>();
			CreateMap<TblTempCorporateAccountAggregation, BankAdminProfileResponse>();
		}
	}
}