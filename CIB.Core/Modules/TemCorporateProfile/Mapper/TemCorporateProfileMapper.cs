using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TemCorporateProfile.Mapper
{
    public class TemCorporateProfileMapper : Profile
    {
        public TemCorporateProfileMapper()
        {
            CreateMap<TblCorporateProfile, TblTempCorporateProfile>();
            CreateMap<TblTempCorporateProfile, TblCorporateProfile>().ReverseMap();
            CreateMap<TblTempCorporateProfile, CorporateProfileResponseDto>().ReverseMap();
        }
    }
}