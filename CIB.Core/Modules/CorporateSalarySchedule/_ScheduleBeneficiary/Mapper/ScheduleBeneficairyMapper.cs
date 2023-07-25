
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto;

namespace CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Mapper
{
   
    public class ScheduleBeneficairyMapper : Profile
    {
        public ScheduleBeneficairyMapper() 
        {
            CreateMap<CreateBeneficiaryRequestDto, TblCorporateSalaryScheduleBeneficiary>().ReverseMap();
         
        }
    
    }
}