using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.LoginLogCorporate
{
    public class LoginLogCorporateRepository : Repository<TblLoginLogCorp>,ILoginLogCorporateRepository
    {
        public LoginLogCorporateRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public void UpdateLoginLogCorporate(TblLoginLogCorp update)
        {
             _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }
  }
}