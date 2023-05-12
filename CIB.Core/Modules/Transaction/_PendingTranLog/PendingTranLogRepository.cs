using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;

namespace CIB.Core.Modules.Transaction._PendingTranLog
{
    public class PendingTranLogRepository : Repository<TblPendingTranLog>,IPendingTranLogRepository
    {
        public PendingTranLogRepository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
        get { return _context as ParallexCIBContext; }
        }

        public IEnumerable<TblPendingTranLog> GetAllCompanyPendingTranLog(Guid companyId)
        {
            return _context.TblPendingTranLogs.Where(a => a.Status == 0 && a.CompanyId == companyId ).OrderByDescending(ctx=> ctx.Sn).ToList();
        }

        public IEnumerable<TblPendingTranLog> GetAllDeclineTransaction(Guid companyId)
        {
            return _context.TblPendingTranLogs.Where(a => a.Status == (int)ProfileStatus.Declined && a.CompanyId == companyId ).OrderByDescending(ctx=> ctx.Sn).ToList();
        }

        public void UpdatePendingTranLog(TblPendingTranLog update)
        {
            _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }

        public IEnumerable<TblPendingTranLog> GetAllCompanySingleTransactionInfo(Guid companyId)
        {
            return _context.TblPendingTranLogs.Where(a => a.CompanyId == companyId &&  a.Status == 0).ToList().OrderByDescending(ctx=> ctx.Sn);
        }

        // public IEnumerable<TblPendingTranLog> GetAllCompanySingleTransactionInfo(Guid companyId)
        // {
        //     return _context.TblPendingTranLogs.Where(a => a.CompanyId == companyId &&  a.Status == 0).ToList().OrderByDescending(ctx=> ctx.Sn);
        // }
  }
}
