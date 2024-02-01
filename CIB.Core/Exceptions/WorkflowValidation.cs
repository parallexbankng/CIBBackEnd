using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Entities;
using CIB.Core.Modules.Transaction.Dto;

namespace CIB.Core.Exceptions
{
  public static class WorkflowValidation
  {
    public static bool Validate(TblWorkflow workflow, List<TblWorkflowHierarchy> hierarchies, decimal? amount, out string errorMessage)
    {
      if (workflow is null)
      {
        errorMessage = "Workflow is invalid";
        return false;
      }
      else
      {
        if (workflow.Status != 1)
        {
          errorMessage = "Workflow selected is not active";
          return false;
        }

        if (!hierarchies.Any())
        {
          errorMessage = "No Workflow Hierarchies found";
          return false;
        }
        if (hierarchies.Count != workflow.NoOfAuthorizers)
        {
          errorMessage = "Workflow Authorize is not valid ";
          return false;
        }
      }
      errorMessage = "Ok ";
      return true;
    }

	
	}
}

