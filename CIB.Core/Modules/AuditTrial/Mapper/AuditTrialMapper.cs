using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.AuditTrial.Dto;

namespace CIB.Core.Modules.AuditTrial.Mapper
{
    public class AuditTrialMapper : Profile
    {
        public AuditTrialMapper()
        {
            CreateMap<AuditTrialDto, TblAuditTrail>();
            //CreateMap<TblAuditTrail, BankAdminProfileResponse>();
        }
    }
}