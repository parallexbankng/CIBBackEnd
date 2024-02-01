using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Entities;
using CIB.Core.Modules.WorkflowHierarchy.Dto;

namespace CIB.Core.Modules.WorkflowHierarchy.Mapper
{
    public class WorkflowHierachyMapper : Profile
    {
          public WorkflowHierachyMapper()
        {
            CreateMap<CreateWorkflowHierarchyDto, TblWorkflowHierarchy>();
            CreateMap<TblWorkflowHierarchy, WorkflowHierarchyResponseDto>();
            CreateMap<TblWorkflowHierarchy, TblTempWorkflowHierarchy>();
            CreateMap<WorkflowHierarchyResponseDto, TblTempWorkflowHierarchy>();
        }
    }
}