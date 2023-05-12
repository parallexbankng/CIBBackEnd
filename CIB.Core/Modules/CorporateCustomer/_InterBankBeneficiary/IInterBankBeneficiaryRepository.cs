using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateCustomer._InterBankBeneficiary
{
    public interface IInterBankBeneficiaryRepository : IRepository<TblInterbankbeneficiary>
    {
        TblInterbankbeneficiary GetInterbankBeneficiaryByAccountNumber(string AccountNumber, Guid corporateCustomerId);
        List<TblInterbankbeneficiary> GetInterbankBeneficiaries(Guid corporateCustomerId);
        List<TblInterbankbeneficiary> GetInterbankBeneficiary(Guid Id, Guid corporateCustomerId);
  }
}