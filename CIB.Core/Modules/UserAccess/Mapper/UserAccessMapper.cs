using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.UserAccess.Dto;

namespace CIB.Core.Modules.UserAccess.Mapper
{
    public class UserAccessProfile : Profile
    {
        public UserAccessProfile()
        {
            CreateMap<CreateRequestDto, TblUserAccess>();
            CreateMap<TblUserAccess, UserAccessResponseDto>();
        }
    }
}