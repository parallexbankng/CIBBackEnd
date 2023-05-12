using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateCustomer._InterBankBeneficiary
{
    public class InterBankBeneficiaryRepository : Repository<TblInterbankbeneficiary>,IInterBankBeneficiaryRepository
    {
        public InterBankBeneficiaryRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }

        public TblInterbankbeneficiary GetInterbankBeneficiaryByAccountNumber(string AccountNumber, Guid corporateCustomerId)
        {
          return _context.TblInterbankbeneficiaries.FirstOrDefault(x => x.CustAuth == corporateCustomerId && x.AccountNumber == AccountNumber);
        }

        public List<TblInterbankbeneficiary> GetInterbankBeneficiaries(Guid corporateCustomerId)
        {
          return _context.TblInterbankbeneficiaries.Where(x => x.CustAuth == corporateCustomerId).OrderByDescending(ctx => ctx.Sn).ToList();
        }

        public List<TblInterbankbeneficiary> GetInterbankBeneficiary(Guid Id, Guid corporateCustomerId)
        {
          return _context.TblInterbankbeneficiaries.Where(x => x.CustAuth == corporateCustomerId && x.Id == Id).OrderByDescending(ctx => ctx.Sn).ToList();
        }
  }
}