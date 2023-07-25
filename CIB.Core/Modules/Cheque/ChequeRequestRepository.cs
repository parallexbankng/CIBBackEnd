using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<TblTempChequeRequest>> GetPendingChequeRequetsByCorporateCustomer(Guid corporateCustomerId)
        {
             return await _context.TblTempChequeRequests.Where(ctx => ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == corporateCustomerId  && ctx.IsTreated == 0).ToListAsync();
        }
        
  }
}