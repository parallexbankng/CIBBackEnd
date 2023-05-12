using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.Workflow
{
    public class WorkFlowRepository : Repository<TblWorkflow>,IWorkFlowRepository
    {
        public WorkFlowRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public void UpdateWorkflow(TblWorkflow update)
        {
        _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }

        public IEnumerable<TblWorkflow> GetAllWorkflows()
        {
            var Workflows = _context.TblWorkflows;
            return Workflows;
        }

        public IEnumerable<TblWorkflow> GetAllWorkflow(Guid corporateCustomerId)
        {
            return  _context.TblWorkflows.Where(x => x.CorporateCustomerId == corporateCustomerId).OrderByDescending(ctx => ctx.Sn).ToList();
           
        }

        public TblWorkflow GetWorkflowByID(Guid id)
        {
            var Workflow = _context.TblWorkflows.SingleOrDefault(a => a.Id == id);
            return Workflow;
        }

        public TblWorkflow GetWorkflowByCorporateCustomerId(Guid id)
        {
            return  _context.TblWorkflows.Where(x => x.CorporateCustomerId == id).FirstOrDefault();
        }

        public DuplicateStatus CheckDuplicate(TblWorkflow profile, bool IsUpdate = false)
        {
            var duplicateEmail = _context.TblWorkflows.FirstOrDefault(x => x.Name.Trim().ToLower().Equals(profile.Name.Trim().ToLower()) && x.CorporateCustomerId != null && x.CorporateCustomerId == profile.CorporateCustomerId);
            if(duplicateEmail != null)
            {
                if(IsUpdate)
                {
                if(profile.Id != duplicateEmail.Id)
                {
                    return new DuplicateStatus { Message = "Work flow Already Exit", IsDuplicate = true };
                }
                }
                else
                {
                return new DuplicateStatus { Message = "Work flow Already Exit or is Pending Approval", IsDuplicate =true};
                }
            }
            return new DuplicateStatus { Message = "", IsDuplicate = false };
        }

   
  }
}