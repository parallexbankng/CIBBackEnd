using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.PasswordHistory
{
    public interface IPasswordHistoryRepository  : IRepository<TblPasswordHistory>
    {
        void UpdatePasswordHistory(TblPasswordHistory update);
        List<TblPasswordHistory> GetPasswordHistoryByCorporateProfileId(string profileId);
    }
}