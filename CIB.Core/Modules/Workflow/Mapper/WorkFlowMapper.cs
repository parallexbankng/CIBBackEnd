using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.Workflow.Dto;

namespace CIB.Core.Modules.Workflow.Mapper
{
    public class WorkFlowMapper : Profile
    {
        public WorkFlowMapper()
        {
            CreateMap<CreateWorkflowDto, TblWorkflow>();
            CreateMap<TblWorkflow, WorkFlowResponseDto>();
            CreateMap<TblWorkflow, TblTempWorkflow>().ReverseMap();
        }
    }
}