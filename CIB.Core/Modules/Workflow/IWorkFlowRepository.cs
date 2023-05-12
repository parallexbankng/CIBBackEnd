using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.Workflow
{
  public interface IWorkFlowRepository : IRepository<TblWorkflow>
  {
    void UpdateWorkflow(TblWorkflow update);
    IEnumerable<TblWorkflow> GetAllWorkflows();
    IEnumerable<TblWorkflow> GetAllWorkflow(Guid corporateCustomerId);
    TblWorkflow GetWorkflowByID(Guid id);
    DuplicateStatus CheckDuplicate(TblWorkflow profile, bool IsUpdate = false);
    TblWorkflow GetWorkflowByCorporateCustomerId(Guid id);
  }
}