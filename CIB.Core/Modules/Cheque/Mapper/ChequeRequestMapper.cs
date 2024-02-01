using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Cheque.Dto;

namespace CIB.Core.Modules.Cheque.Mapper
{
    public class ChequeRequestProfileMapper  : Profile
    {
        public ChequeRequestProfileMapper()
        {
            CreateMap<TblChequeRequest, RequestChequeBook>().ReverseMap();
            CreateMap<TblChequeRequest, ResponseChequeBookDto>().ReverseMap();
        }
    }
}