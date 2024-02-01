using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateCustomer._IntraBankBeneficiary
{
    public interface IIntraBankBeneficiaryRepository  : IRepository<TblIntrabankbeneficiary>
    {
    TblIntrabankbeneficiary GetIntrabankBeneficiaryByAccountNumber(string AccountNumber, Guid corporateCustomerId);
    List<TblIntrabankbeneficiary> GetIntrabankBeneficiaries(Guid corporateCustomerId);
     List<TblIntrabankbeneficiary> GetIntrabankBeneficiary(Guid Id, Guid corporateCustomerId);
  }
}