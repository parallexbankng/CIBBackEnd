
using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Modules.TemBankAdminProfile
{
    public interface ITemBankAdminProfileRepository : IRepository<TblTempBankProfile>
    {
        List<TblTempBankProfile> GetBankProfilePendingApprovals(int isTreated);
        TblTempBankProfile GetBankProfilePendingApproval(TblBankProfile profile,int isTreated);
        List<TblTempBankProfile> CheckDuplicateRequest(TblBankProfile profile,string Action);
        AdminUserStatus CheckDuplicate(TblBankProfile update, Guid? profileId = null);
        AdminUserStatus CheckDuplicate(TblTempBankProfile profile, bool isUpdate);
        void UpdateTemBankAdminProfile(TblTempBankProfile update);
    }
}