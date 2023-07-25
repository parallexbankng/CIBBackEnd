using System;
using CIB.Core.Entities;
using CIB.Core.Enums;

namespace CIB.Core.Exceptions
{
  public static class ValidationPermission
  {
    public static bool IsAuthorized(TblCorporateCustomer corporateCustomer, out string errorMessage)
    {
      if (Enum.TryParse(corporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
      {
        if (authorizationType != AuthorizationType.Single_Signatory)
        {
          errorMessage = "UnAuthorized Access";
          return false;
        }
      }
      else
      {
        errorMessage = "Authorization type could not be determined!!!\"";
        return false;
      }

      errorMessage = "Ok";
      return true;

    }

    public static bool IsValidCorporateCustomer(TblCorporateCustomer tblCorporate, TblCorporateProfile profile, out string errorMessage)
    {
      if (tblCorporate == null || profile.CorporateCustomerId != tblCorporate.Id)
      {
        errorMessage = "UnAuthorized Access";
        return false;
      }
      errorMessage = "Ok";
      return true;
    }
  }
}

