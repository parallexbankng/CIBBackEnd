using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee.Dto;

namespace CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee.Mapper
{
    public class CorporateEmployeeMapper : Profile
    {
        public CorporateEmployeeMapper() 
        {
            //CreateMap<CreateCorporateEmployeeDto, TblCorporateCustomerEmployee>().ReverseMap();
            CreateMap<TblCorporateSalarySchedule, CorporateEmployeeResponse>().ReverseMap();
            CreateMap<TblTempCorporateCustomerEmployee, CorporateEmployeeResponse>().ReverseMap();
            CreateMap<TblTempCorporateCustomerEmployee, TempCorporateEmployeeResponse>().ReverseMap();

        }
    
    }
}