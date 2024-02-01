using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common.Interface;

namespace CIB.TransactionReversalService.Modules.BulkPaymentLog;

  public interface IBulkPaymentLogRepository : IRepository<TblNipbulkTransferLog>
  {
      List<TblNipbulkTransferLog> GetPendingTransferItems(int status, int perProcess, int tryCount,DateTime proccessDate);
      AccountInfo GetAccountInfo(Guid tranId);
      void UpdateStatus(TblNipbulkTransferLog status);
      List<TblNipbulkTransferLog> CheckInterBankStatus(Guid? tranId, int isPending);
  }

public record AccountInfo
{
  public string? SourceAccountName { get; set; }
  public string? SourceAccountNumber { get; set; }
  public string? SuspenseAccountName{ get; set; }
  public string? SuspenseAccountNumber{ get; set; }
  public string? InterBankSuspenseAccountName{ get; set; }
  public string? InterBankSuspenseAccountNumber{ get; set; }
  public string? TransactionLocation{ get; set; }
  public string? UserName { get; set; }
  public string? BankCode { get; set; }
}



