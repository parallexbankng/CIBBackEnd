using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.CorporateProfile.Mapper
{
    public class CorporateProfileMapper:Profile
    {
        public CorporateProfileMapper()
        {
            CreateMap<CreateProfileDto, TblCorporateProfile>();
            CreateMap<OnboardCorporateCustomerRequestDto, TblCorporateProfile>();
            CreateMap<TblCorporateProfile, CorporateProfileResponseDto>();
            CreateMap<UpdateProfileDTO, TblCorporateProfile>();
            CreateMap<TblTempCorporateProfile, TblCorporateProfile>().ReverseMap();
        }
    }
}