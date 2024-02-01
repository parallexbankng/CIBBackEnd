using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.TokenBlackCoporate
{
    public interface ITokenBlackCorporateRepository : IRepository<TblTokenBlackCorp>
    {
        void UpdateTokenBlackCorporate(TblTokenBlackCorp update);
        List<TblTokenBlackCorp> GetBlackTokenById(Guid Id);
        bool IsTokenStillValid(Guid UserId, string Token);
        TblTokenBlackCorp GetTokenByUserId(Guid userId);
    }
}