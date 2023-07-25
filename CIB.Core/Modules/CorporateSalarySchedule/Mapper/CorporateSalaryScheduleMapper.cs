using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;

namespace CIB.Core.Modules.CorporateSalarySchedule.Mapper
{
    public class CorporateSalaryScheduleMapper : Profile
    {
        public CorporateSalaryScheduleMapper() 
        {
            CreateMap<CreateCorporateCustomerSalaryDto, TblCorporateSalarySchedule>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, CorporateCustomerSalaryResponseDto>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, CreateCorporateCustomerSalaryDto>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, UpdateCorporateCustomerSalaryDto>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, TblTempCorporateSalarySchedule>().ReverseMap();

             
            
        }
    }
}