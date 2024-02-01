
using CIB.IntraBankTransactionService.Modules.Common.Interface;
using CIB.IntraBankTransactionService.Entities;

namespace CIB.IntraBankTransactionService.Modules.BulkCreditLog;

public interface IBulkCreditLogRepository  : IRepository<TblNipbulkCreditLog>
{
  List<TblNipbulkCreditLog> GetPendingCredit(Guid tranLogId, int status, string bankCode, DateTime processDate);
  List<TblNipbulkCreditLog> CheckForPendingCredit(Guid tranLogId, int status, DateTime processDate);
  void UpdateCreditStatus(TblNipbulkCreditLog status);
}
