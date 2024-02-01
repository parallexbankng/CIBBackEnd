using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.WorkflowHierarchy
{
  public interface IWorkflowHierarchyRepository : IRepository<TblWorkflowHierarchy>
  {
    void UpdateWorkflowHierarchy(TblWorkflowHierarchy update);
    List<TblWorkflowHierarchy> GetWorkflowHierarchiesByWorkflowId(Guid workflowID);
    TblWorkflowHierarchy CheckDuplicateHierarchies(Guid workflowId, Guid approvalId);
    TblWorkflowHierarchy GetWorkflowHierarchyByWorkflowId(Guid workflowId);
    TblWorkflowHierarchy GetWorkFlowApprovalHierarchy(Guid workflowId, Guid approvalId);
  }
}