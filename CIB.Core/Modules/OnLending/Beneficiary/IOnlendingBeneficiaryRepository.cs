using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.OnLending.Beneficiary
{
    public interface IOnlendingBeneficiaryRepository : IRepository<TblOnlendingBeneficiary>
    {
        void UpdateOnlendingBeneficiary(TblOnlendingBeneficiary update);
    }
}