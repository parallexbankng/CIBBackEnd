
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Modules.Common.Interface;

namespace CIB.IntraBankTransactionService.Modules.BulkPaymentLog;

  public interface IBulkPaymentLogRepository : IRepository<TblNipbulkTransferLog>
  {
      List<TblNipbulkTransferLog> GetPendingTransferItems(int status, int perProcess,int tryCount);
      void UpdateStatus(TblNipbulkTransferLog status);
      List<TblNipbulkTransferLog> CheckInterBankStatus(Guid? tranId, int isPending);
      int GetInterBankTotalCredit(Guid tranLogId,string bankCode, DateTime processDate);
  }
