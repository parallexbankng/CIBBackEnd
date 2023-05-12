using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateCustomer._IntraBankBeneficiary
{
    public class IntraBankBeneficiaryRepository: Repository<TblIntrabankbeneficiary>,IIntraBankBeneficiaryRepository
    {
        public IntraBankBeneficiaryRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }

        public TblIntrabankbeneficiary GetIntrabankBeneficiaryByAccountNumber(string AccountNumber, Guid corporateCustomerId)
        {
          return _context.TblIntrabankbeneficiaries.FirstOrDefault(x => x.CustAuth == corporateCustomerId && x.AccountNumber == AccountNumber);
        }

        public List<TblIntrabankbeneficiary> GetIntrabankBeneficiaries(Guid corporateCustomerId)
        {
          return _context.TblIntrabankbeneficiaries.Where(x => x.CustAuth == corporateCustomerId).ToList();
        }

        public List<TblIntrabankbeneficiary> GetIntrabankBeneficiary(Guid Id, Guid corporateCustomerId)
        {
          return _context.TblIntrabankbeneficiaries.Where(x => x.CustAuth == corporateCustomerId && x.Id == Id).ToList();
        }

  }
}