using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.Cheque
{
    public class ChequeRequestRepository :Repository<TblChequeRequest>, IChequeRequestRepository
    {
        public ChequeRequestRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public List<TblChequeRequest> GetChequeRequetsByCorporateCustomer(Guid corporateCustomerId)
        {
            return _context.TblChequeRequests.Where(ctx => ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == corporateCustomerId).ToList();
        }

        public List<TblChequeRequest> GetChequeRequestList(int status)
        {
            return _context.TblChequeRequests.Where(ctx => ctx.Status == status).ToList();
        }
  }
}