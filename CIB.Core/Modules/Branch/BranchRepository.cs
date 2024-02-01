
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using System.Linq;

namespace CIB.Core.Modules.Branch
{
    public class BranchRepository : Repository<TblBankBranch>, IBranchRepository
    {
        public BranchRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
            get { return _context as ParallexCIBContext; }
        }

        public TblBankBranch GetBranchById(long Id)
        {
            return _context.TblBankBranches.Where(ctx => ctx.Id == Id).FirstOrDefault();
        }
  }
}