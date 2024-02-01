using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TemCorporateProfile
{
    public interface ITemCorporateProfileRepository : IRepository<TblTempCorporateProfile>
    {
        List<TblTempCorporateProfile> GetCorporateProfilePendingApproval(int isTreated, Guid CorporateCustormerId);
        List<TblTempCorporateProfile> GetCorporateProfilePendingApproval(int isTreated);
        TblTempCorporateProfile CheckDuplicateUserName(string userName);
        void UpdateTempCorporateProfile(TblTempCorporateProfile update);
        CorporateUserStatus CheckDuplicate(TblTempCorporateProfile profile, Guid CorporateCustomerId, bool isUpdate = false);
    }
}