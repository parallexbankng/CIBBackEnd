using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TempWorkFlow
{
    public interface ITempWorkflowRepository : IRepository<TblTempWorkflow>
    {
        DuplicateStatus CheckDuplicate(TblTempWorkflow profile, bool IsUpdate = false);
        List<TblTempWorkflow> CheckDuplicateRequest(TblWorkflow profile,string Action);
        List<TblTempWorkflow> CheckTempDuplicateRequest(TblTempWorkflow profile,string Action);
        void UpdateTempWorkflow(TblTempWorkflow update);
        TblTempWorkflow CheckTempWorkflowDuplicate(string workflowName, Guid? CorporateCustomerId);
        List<TblTempWorkflow> GetTempWorkflowPendingApproval(int isTreated);
        List<TblTempWorkflow> GetCorporateTempWorkflowPendingApproval(int isTreated, Guid CorporateCustomerId);
    }
}