using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateCustomer.Dto;

namespace CIB.Core.Modules.CorporateCustomer.Mapper
{
    public class CorporateCustomerMapper : Profile
    {
        public CorporateCustomerMapper()
        {
            CreateMap<CreateCorporateCustomerRequestDto, TblCorporateCustomer>();
            CreateMap<OnboardCorporateCustomer, TblCorporateCustomer>();
            CreateMap<TblCorporateCustomer, CorporateCustomerResponseDto>();
            CreateMap<CorporateCustomerResponseDto, TblCorporateCustomer>();
            
        }
    }
}