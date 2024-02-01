using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Branch.Dto;

namespace CIB.Core.Modules.Branch.Mapper
{
    public class BranchProfileMapper  : Profile
    {
    
        public BranchProfileMapper()
        {
            
            CreateMap<TblBankProfile, BankBranchResponse>().ReverseMap();
        }
    
    }
}