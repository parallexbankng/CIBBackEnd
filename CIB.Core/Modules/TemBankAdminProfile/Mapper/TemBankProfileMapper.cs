using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Modules.TemBankAdminProfile.Mapper
{
    public class TemBankProfileMapper : Profile
    {
     
        public TemBankProfileMapper()
        {
            CreateMap<TblBankProfile, TblTempBankProfile>();
            CreateMap<TblTempBankProfile, BankAdminProfileResponse>();
            CreateMap<TblTempBankProfile, TblBankProfile>();

            //TblTempBankProfile
        }
    
    }
}