using System;

using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.WorkflowHierarchy
{
    public class WorkflowHierarchyRespository : Repository<TblWorkflowHierarchy>,IWorkflowHierarchyRepository
    {
      public WorkflowHierarchyRespository(ParallexCIBContext context) : base(context)
      {
      }
      public ParallexCIBContext context
      {
        get { return _context as ParallexCIBContext; }
      }

      public TblWorkflowHierarchy CheckDuplicateHierarchies(Guid workflowId, Guid approvalId)
      {
        return _context.TblWorkflowHierarchies.Where(ctx => ctx.WorkflowId == workflowId && ctx.ApproverId == approvalId).FirstOrDefault();
      }

    public TblWorkflowHierarchy GetWorkFlowApprovalHierarchy(Guid workflowId, Guid approvalId)
    {
       return _context.TblWorkflowHierarchies.Where(ctx => ctx.WorkflowId  == workflowId && ctx.ApproverId == approvalId).FirstOrDefault();
    }

    public  List<TblWorkflowHierarchy> GetWorkflowHierarchiesByWorkflowId(Guid workflowID)
    {
      return _context.TblWorkflowHierarchies.Where(ctx => ctx.WorkflowId == workflowID).ToList();
    }

    public TblWorkflowHierarchy GetWorkflowHierarchyByWorkflowId(Guid workflowId)
    {
      return _context.TblWorkflowHierarchies.Where(ctx => ctx.WorkflowId == workflowId).FirstOrDefault();
    }

    public void UpdateWorkflowHierarchy(TblWorkflowHierarchy update)
    {
      _context.Update(update).Property(x=>x.Sn).IsModified = false;
    }
  }
}