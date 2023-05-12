
using CIB.InterBankTransactionService.Modules.Common.Interface;
using CIB.InterBankTransactionService.Entities;

namespace CIB.InterBankTransactionService.Modules.BulkCreditLog;

public interface IBulkCreditLogRepository  : IRepository<TblNipbulkCreditLog>
{
  List<TblNipbulkCreditLog> GetPendingCredit(Guid tranLogId, int status, string bankCode, DateTime processDate);
  List<TblNipbulkCreditLog> CheckForPendingCredit(Guid tranLogId, int status, DateTime processDate);
  int GetInterBankTotalCredit(Guid tranLogId,string bankCode, DateTime processDate);
  void UpdateCreditStatus(TblNipbulkCreditLog status);
}
