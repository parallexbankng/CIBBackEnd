using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.CorporateUserRoleAccess.Mapper
{
 
    public class CorporateUserRoleAccessMapper : Profile
    {
       public CorporateUserRoleAccessMapper()
        {
            CreateMap<UserAccessModel, TblCorporateRoleUserAccess>();
            CreateMap<TblCorporateRoleUserAccess, UserAccessModel>();
            
        }
    }
}