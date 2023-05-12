
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Role.Dto;

namespace CIB.Core.Modules.Role.Mapper
{
    public class RoleMapper : Profile
    {
        public RoleMapper()
        {
            CreateMap<CreateRoleDto, TblRole>();
            CreateMap<TblRole, RoleResponseDto>();
        }
    }
}