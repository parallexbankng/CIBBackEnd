using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.OnLending.Beneficiary
{
    public class OnlendingBeneficiaryRepository : Repository<TblOnlendingBeneficiary>, IOnlendingBeneficiaryRepository
    {
        public OnlendingBeneficiaryRepository(ParallexCIBContext context) : base(context)
        {
        }
        public ParallexCIBContext context
        {
            get { return _context as ParallexCIBContext; }
        }

        public void UpdateOnlendingBeneficiary(TblOnlendingBeneficiary update)
        {
            _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }
  }
}