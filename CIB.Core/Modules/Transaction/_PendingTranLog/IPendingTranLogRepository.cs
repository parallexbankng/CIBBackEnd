using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.Transaction._PendingTranLog
{
    public interface IPendingTranLogRepository : IRepository<TblPendingTranLog>
    {
      void UpdatePendingTranLog(TblPendingTranLog update);
      IEnumerable<TblPendingTranLog> GetAllCompanyPendingTranLog(Guid companyId);
      IEnumerable<TblPendingTranLog> GetAllCompanySingleTransactionInfo(Guid companyId);
      IEnumerable<TblPendingTranLog> GetAllDeclineTransaction(Guid companyId);
      
    }
}