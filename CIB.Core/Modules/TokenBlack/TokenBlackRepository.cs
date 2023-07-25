
using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.TokenBlack
{
    public class TokenBlackRepository: Repository<TblTokenBlack>, ITokenBlackRepository
    {
        public TokenBlackRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }

      public void UpdateTokenBlack(TblTokenBlack update)
      {
        _context.Update(update).Property(x=>x.Sn).IsModified = false;
      }

    public List<TblTokenBlack> GetBlackTokenById(Guid Id)
    {
      return _context.TblTokenBlacks.Where(ctx => ctx.CustAutId == Id && ctx.IsBlack != 1).ToList();
    }

    public void UpdateTokenBlackRange(List<TblTokenBlack> update)
    {
      _context.UpdateRange(update);
    }

    public bool IsTokenStillValid(Guid UserId, string Token)
    {
      var tokenBlack = _context.TblTokenBlacks.Where(a => a.CustAutId == UserId && a.TokenCode == Token)?.FirstOrDefault();
      if (tokenBlack != null)
      {
          if (tokenBlack.IsBlack == 0) return true;
      }
      return false;
    }
    public TblTokenBlack GetTokenByUserId(Guid userId)
    {
      return _context.TblTokenBlacks.Where(ctx => ctx.CustAutId != null && ctx.CustAutId == userId && ctx.IsBlack == 0).FirstOrDefault();
    }

  }
}