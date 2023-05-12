using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.TokenBlackCoporate
{
    public class TokenBlackCorporateRepository: Repository<TblTokenBlackCorp>, ITokenBlackCorporateRepository
    {
        public TokenBlackCorporateRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }

      public List<TblTokenBlackCorp> GetBlackTokenById(Guid Id)
      {
        return _context.TblTokenBlackCorps.Where(ctx => ctx.CustAutId == Id && ctx.IsBlack != 1).ToList();
      }

      public bool IsTokenStillValid(Guid UserId, string Token)
      {
        var tokenBlack = _context.TblTokenBlackCorps.Where(a => a.CustAutId == UserId && a.TokenCode == Token)?.FirstOrDefault();
        if (tokenBlack != null)
        {
            if (tokenBlack.IsBlack == 0) return true;
        }
        return false;
      }

    public void UpdateTokenBlackCorporate(TblTokenBlackCorp update)
      {
        _context.Update(update).Property(x=>x.Sn).IsModified = false;
      }
}
}