
using CIB.TransactionReversalService.Modules.Common.Interface;
using CIB.TransactionReversalService.Entities;

namespace CIB.TransactionReversalService.Modules.BulkCreditLog;

public interface IBulkCreditLogRepository  : IRepository<TblNipbulkCreditLog>
{
  List<TblNipbulkCreditLog> GetFailedTransaction(Guid tranId,int status, int retryCount, int totalPerProcess, DateTime processDate);
  void UpdateCreditStatus(TblNipbulkCreditLog status);
}
