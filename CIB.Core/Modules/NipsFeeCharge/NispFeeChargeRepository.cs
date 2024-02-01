using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.NipsFeeCharge
{
    public class NispFeeChargeRepository : Repository<TblFeeCharge>, INipsFeeChargeRepository
    {
        public NispFeeChargeRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
            get { return _context as ParallexCIBContext; }
        }
    }
}