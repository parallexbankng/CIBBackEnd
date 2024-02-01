
using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.PasswordHistory;

namespace CIB.Core.Modules.PasswordHistory
{
    public class PasswordHistoryRepository : Repository<TblPasswordHistory>, IPasswordHistoryRepository 
    {
        public PasswordHistoryRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public void UpdatePasswordHistory(TblPasswordHistory update)
        {
           _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }

        public List<TblPasswordHistory> GetPasswordHistoryByCorporateProfileId(string profileId)
        {
           return _context.TblPasswordHistories.Where(ctx => ctx.CustomerProfileId == profileId).ToList();
        }
  }
}