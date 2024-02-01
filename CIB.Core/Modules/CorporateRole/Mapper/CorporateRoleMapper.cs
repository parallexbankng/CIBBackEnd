using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Modules.CorporateRole.Dto;

namespace CIB.Core.Modules.CorporateRole.Mapper
{
    public class CorporateRoleMapper : Profile
    {
       public CorporateRoleMapper()
        {
            CreateMap<CreateCorporateRoleDto, TblCorporateRole>();
            CreateMap<TblCorporateRole, CorporateRoleResponseDto>();
            
        }
    }
}