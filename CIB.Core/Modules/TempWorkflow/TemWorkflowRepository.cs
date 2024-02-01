using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateProfile.Dto;
using CIB.Core.Modules.TempWorkFlow;

namespace CIB.Core.Modules.TempWorkflow
{
    public class TemWorkflowRepository : Repository<TblTempWorkflow>, ITempWorkflowRepository
    {
        public TemWorkflowRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }
        public DuplicateStatus CheckDuplicate(TblTempWorkflow profile, bool IsUpdate)
        {
            var checkDuplicate = _context.TblTempWorkflows.FirstOrDefault(x => x.Name.Trim().ToLower().Equals(profile.Name.Trim().ToLower()) && x.CorporateCustomerId != null && x.CorporateCustomerId == profile.CorporateCustomerId);
            if(checkDuplicate != null)
            {
                if(IsUpdate)
                {
                if(profile.Id != checkDuplicate.Id)
                {
                    return new DuplicateStatus { Message = "Workflow Already Exit", IsDuplicate = true };
                }
                }
                else
                {
                    return new DuplicateStatus { Message = "Workflow Already Exit or is Pending Approval", IsDuplicate =true};
                }
            }
            return new DuplicateStatus { Message = "", IsDuplicate = false };
        }


        public List<TblTempWorkflow> CheckDuplicateRequest(TblWorkflow profile, string Action)
        {
            return _context.TblTempWorkflows.Where(ctx => ctx.IsTreated == (int)ProfileStatus.Pending  && ctx.CorporateCustomerId == profile.CorporateCustomerId && ctx.Id != profile.Id).ToList();  
        }

        public TblTempWorkflow CheckTempWorkflowDuplicate(string workflowName, Guid? CorporateCustomerId)
        {
            return _context.TblTempWorkflows.FirstOrDefault(ctx => ctx.IsTreated == (int)ProfileStatus.Pending &&  ctx.Name != null && ctx.Name.ToLower().Trim() == workflowName.Trim().ToLower()  && ctx.CorporateCustomerId == CorporateCustomerId);  
        }
        
        public List<TblTempWorkflow> CheckTempDuplicateRequest(TblTempWorkflow profile, string Action)
        {
            return _context.TblTempWorkflows.Where(ctx => ctx.IsTreated == (int)ProfileStatus.Pending && ctx.Name == profile.Name && ctx.CorporateCustomerId == profile.CorporateCustomerId && ctx.WorkflowId == profile.WorkflowId && ctx.Id != profile.Id).ToList();
        }
        
        public void UpdateTempWorkflow(TblTempWorkflow update)
        {
            _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }
        public List<TblTempWorkflow> GetTempWorkflowPendingApproval(int isTreated)
        {
            return _context.TblTempWorkflows.Where(ctx => ctx.IsTreated == isTreated).ToList();
        }

        public List<TblTempWorkflow> GetCorporateTempWorkflowPendingApproval(int isTreated, Guid CorporateCustomerId)
        {
            return _context.TblTempWorkflows.Where(ctx => ctx.IsTreated == isTreated && ctx.CorporateCustomerId == CorporateCustomerId).ToList();
        }
        
    
    }
}