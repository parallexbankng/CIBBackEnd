using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.TokenBlack
{
    public interface ITokenBlackRepository : IRepository<TblTokenBlack>
    {
        void UpdateTokenBlack(TblTokenBlack update);
        void UpdateTokenBlackRange(List<TblTokenBlack> update);
        List<TblTokenBlack> GetBlackTokenById(Guid Id);
        bool IsTokenStillValid(Guid UserId, string Token);
    }
}