using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.Transaction._NipbulkCreditLog
{
    public interface INipBulkCreditLogRepository:IRepository<TblNipbulkCreditLog>
    {
        List<TblNipbulkCreditLog> GetbulkCreditLog(Guid TranLog);
        List<TblNipbulkCreditLog> GetbulkCreditLogStatus(Guid TranLog ,int Status);
        void UpdateBulkCreditLog(TblNipbulkCreditLog update);
        void UpdateBulkCreditLogList(IEnumerable<TblNipbulkCreditLog> update);
  }
}