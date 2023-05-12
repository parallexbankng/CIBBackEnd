using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TempWorkflowHierarchy
{
    public class TempWorkflowHierarchyRepository : Repository<TblTempWorkflowHierarchy>, ITempWorkflowHierarchyRepository
    {
        public TempWorkflowHierarchyRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }
        public List<TblTempWorkflowHierarchy> CheckDuplicateRequest(TblWorkflowHierarchy profile, string Action)
        {
          throw new NotImplementedException();
        }

        public void UpdateTempWorkflowHierarchy(TblTempWorkflowHierarchy update)
        {
          _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }

    public DuplicateStatus CheckDuplicate(TblTempWorkflowHierarchy profile, bool IsUpdate)
    {
      throw new NotImplementedException();
    }

    public List<TblTempWorkflowHierarchy> GetTempWorkflowHierarchyByWorkflowId(Guid workflowId)
    {
      return _context.TblTempWorkflowHierarchies.Where(ctx => ctx.WorkflowId  == workflowId && ctx.WorkflowId != null).ToList();
    }

    // public bool CheckDuplicateAuthorizer(Guid workFlowId, Guid userId)
    // {
    //   return _context.TblTempWorkflowHierarchies.Where(ctx => ctx.WorkflowId  == workflowId && ctx.WorkflowId != null).ToList();
    // }
  }
    
}