using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.UserRoleAccess.Dto;

namespace CIB.Core.Modules.UserRoleAccess.Mapper
{
    public class UserRoleAccessMapper:Profile
    {
        public UserRoleAccessMapper(){
        CreateMap<AddRoleAccessRequestDto, TblRoleUserAccess>();
        CreateMap<TblRoleUserAccess, RoleAccessResponseDto>();
        }
       
    }
}