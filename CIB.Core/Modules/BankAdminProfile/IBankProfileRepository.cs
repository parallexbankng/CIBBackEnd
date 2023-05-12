using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Modules.BankAdminProfile
{
    public interface IBankProfileRepository : IRepository<TblBankProfile>
    {
      IEnumerable<BankAdminProfileResponse> GetAllBankAdminProfiles();
      IEnumerable<TblBankProfile> GetAllBankAdminProfilesByRole(string role);
      BankAdminProfileResponse GetBankAdminProfileById(Guid id);
      void UpdateBankProfile(TblBankProfile update);
      AdminUserStatus CheckDuplicate(TblBankProfile update, Guid? profileId = null);
      AdminUserStatus CheckDuplicates(TblBankProfile profile, bool isUpdate = false);
      TblBankProfile GetProfileByUserName(string userName);
      TblBankProfile GetProfileByEmail(string email);
  }
}

