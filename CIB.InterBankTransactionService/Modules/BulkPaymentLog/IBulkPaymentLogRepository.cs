
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.Common.Interface;

namespace CIB.InterBankTransactionService.Modules.BulkPaymentLog;

  public interface IBulkPaymentLogRepository : IRepository<TblNipbulkTransferLog>
  {
      List<TblNipbulkTransferLog> GetPendingTransferItems(int status, int perProcess, int tryCount);
      void UpdateStatus(TblNipbulkTransferLog status);
      List<TblNipbulkTransferLog> CheckInterBankStatus(Guid? tranId, int isPending);
  }
