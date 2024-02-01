using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateCustomer.Dto;

namespace CIB.Core.Modules.TemCorporateCustomer.Mapper
{
    public class TemCorporateCustomerMapper : Profile
    {
        public TemCorporateCustomerMapper()
        {
            CreateMap<TblCorporateCustomer, TblTempCorporateCustomer>();
            CreateMap<TblTempCorporateCustomer, TblCorporateCustomer>();
            CreateMap<OnboardCorporateCustomer, TblTempCorporateCustomer>();
            //CreateMap<TblTempBankProfile, BankAdminProfileResponse>();
        }
    }
}