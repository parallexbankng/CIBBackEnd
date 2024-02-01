using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TempWorkflowHierarchy
{
    public interface ITempWorkflowHierarchyRepository : IRepository<TblTempWorkflowHierarchy>
    {
        DuplicateStatus CheckDuplicate(TblTempWorkflowHierarchy profile, bool IsUpdate = false);
        List<TblTempWorkflowHierarchy> CheckDuplicateRequest(TblWorkflowHierarchy profile,string Action);
        List<TblTempWorkflowHierarchy> GetTempWorkflowHierarchyByWorkflowId(Guid workflowId);
        //bool CheckDuplicateAuthorizer(Guid workFlowId, Guid userId);
        void UpdateTempWorkflowHierarchy(TblTempWorkflowHierarchy update);
       // bool CheckDuplicateAuthorizer(Guid workFlowId, Guid userId);
    }
}
