using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.Transaction._NipbulkTransferLog
{
    public interface INipBulkTransferLogRespository: IRepository<TblNipbulkTransferLog>
    {
      List<TblNipbulkTransferLog> GetBulkPendingTransferLog(Guid CorporateCustomerId);
      List<TblNipbulkTransferLog> GetAuthorizedBulkTransactions(Guid CorporateCustomerId);
      List<TblNipbulkTransferLog> GetCompanyBulkTransactions(Guid CorporateCustomerId);
      List<TblNipbulkTransferLog> GetAllDeclineTransaction(Guid CorporateCustomerId);
      void UpdatebulkTransfer(TblNipbulkTransferLog transferLog);
  }
}
