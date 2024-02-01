using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TemCorporateCustomer
{
    public interface ITemCorporateCustomerRespository : IRepository<TblTempCorporateCustomer>
    {
        List<TblTempCorporateCustomer> GetCorporateCustomerPendingApproval(int isTreated);
        void UpdateTemCorporateCustomer(TblTempCorporateCustomer update);
        CorporateUserStatus CheckDuplicate(TblTempCorporateCustomer profile, bool IsUpdate = false);

        TblTempCorporateCustomer GetCorporateCustomerByCustomerID(string id);
        TblTempCorporateCustomer GetCorporateCustomerByCustomerIDForOnboarding(string id, string action);
        TblTempCorporateCustomer GetCorporateCustomerByCustomerByShortName(string corporateShortName);

        List<TblTempCorporateCustomer> CheckDuplicateRequest(TblCorporateCustomer profile, string action);
    }
}