using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.PasswordReset
{
    public class PasswordResetRepository : Repository<TblPasswordReset>, IPasswordResetRepository
    {
        public PasswordResetRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public void UpdatePasswordReset(TblPasswordReset update)
        {
             _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }
  }
}