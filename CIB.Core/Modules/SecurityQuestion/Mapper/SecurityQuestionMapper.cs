using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.SecurityQuestion.Dto;

namespace CIB.Core.Modules.SecurityQuestion.Mapper
{
    public class SecurityQuestionMapper : Profile
    {
        public SecurityQuestionMapper()
        {
            //CreateMap<SecurityQuestionResponseDto, TblSecurityQuestion>();
            CreateMap<TblSecurityQuestion, SecurityQuestionResponseDto>();
        }
    }
}