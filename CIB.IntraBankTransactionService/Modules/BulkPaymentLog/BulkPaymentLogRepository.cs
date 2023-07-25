
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Modules.Common.Repository;

namespace CIB.IntraBankTransactionService.Modules.BulkPaymentLog;

public class BulkPaymentLogRepository : Repository<TblNipbulkTransferLog>, IBulkPaymentLogRepository
{
  public BulkPaymentLogRepository(ParallexCIBContext context) : base(context)
  {
  }
  public ParallexCIBContext Context { get { return _context as ParallexCIBContext; } }

  public List<TblNipbulkTransferLog> GetPendingTransferItems(int status, int perProcess, int tryCount)
  {
    return _context.TblNipbulkTransferLogs.Where(ctx => ctx.TransactionStatus == status && ctx.ApprovalStatus == 1 && ctx.IntraBankStatus == 0 && ctx.TryCount < tryCount).OrderBy(a => a.TryCount).Take(perProcess).ToList();
  }
  public List<TblNipbulkTransferLog> CheckInterBankStatus(Guid? tranId, int isPending)
  {
    return _context.TblNipbulkTransferLogs.Where(ctx => ctx.Id == tranId && ctx.ApprovalStatus == 1 && ctx.InterBankStatus == 0).ToList();
  }

  public int GetInterBankTotalCredit(Guid tranLogId, string bankCode, DateTime processDate)
  {
    return _context.TblNipbulkCreditLogs.Count(ctx =>
      ctx.InitiateDate != null &&
      ctx.TranLogId == tranLogId &&
      ctx.CreditStatus == 1 &&
      ctx.CreditBankCode == bankCode &&
      ctx.NameEnquiryStatus == 1 &&
      ctx.InitiateDate.Value.Date == processDate);
  }

  public void UpdateStatus(TblNipbulkTransferLog status)
  {
    _context.Update(status).Property(x => x.Sn).IsModified = false;
  }
}
