
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Cheque.Dto;

namespace CIB.Core.Modules.Cheque.Mapper
{
    public class TempChequeRequestProfileMapper  : Profile
    {
        public TempChequeRequestProfileMapper()
        {
            CreateMap<TblTempChequeRequest, RequestChequeBookDto>().ReverseMap();
            CreateMap<TblTempChequeRequest, RequestChequeBookHistory>().ReverseMap();
            CreateMap<TblChequeRequest, ResponseChequeBookDto>().ReverseMap();
            CreateMap<TblChequeRequest, TempResponseChequeBookDto>().ReverseMap();
            CreateMap<TblTempChequeRequest, TblChequeRequest>().ReverseMap();
        }
    }
}