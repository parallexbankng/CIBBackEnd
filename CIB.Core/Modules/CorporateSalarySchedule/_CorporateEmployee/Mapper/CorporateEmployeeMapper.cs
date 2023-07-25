using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;

namespace CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Mapper
{
    public class CorporateEmployeeMapper : Profile
    {
        public CorporateEmployeeMapper() 
        {
            CreateMap<CreateCorporateEmployeeDto, TblCorporateCustomerEmployee>().ReverseMap();
            CreateMap<TblCorporateCustomerEmployee, TblTempCorporateCustomerEmployee>().ReverseMap();
            CreateMap<TblCorporateCustomerEmployee, UpdateCorporateEmployeeDto>().ReverseMap();
            CreateMap<CreateCorporateEmployeeDto, TblTempCorporateCustomerEmployee>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, CorporateEmployeeResponse>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, CorporateEmployeeResponseDto>().ReverseMap();
        }
    
    }
}