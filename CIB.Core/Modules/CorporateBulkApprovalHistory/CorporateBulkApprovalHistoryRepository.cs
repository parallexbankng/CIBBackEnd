using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.CorporateBulkApprovalHistory
{
    public class CorporateBulkApprovalHistoryRepository : Repository<TblCorporateBulkApprovalHistory>,ICorporateBulkApprovalHistoryRepository
    {
        public CorporateBulkApprovalHistoryRepository(ParallexCIBContext context) : base(context)
        {
        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public List<TblCorporateBulkApprovalHistory> GetCorporateBulkAuthorizationHistories(Guid BulkTranId)
        {
            return _context.TblCorporateBulkApprovalHistories.Where(ctx => ctx.LogId == BulkTranId).ToList();
        }
  }
}