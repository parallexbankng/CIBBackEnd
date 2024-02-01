
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.Common.Repository;
using CIB.InterBankTransactionService.Utils;

namespace CIB.InterBankTransactionService.Modules.BulkPaymentLog;

public class BulkPaymentLogRepository : Repository<TblNipbulkTransferLog>, IBulkPaymentLogRepository
{
  public BulkPaymentLogRepository(ParallexCIBContext context) : base(context)
  {
  }
  public ParallexCIBContext Context { get { return _context as ParallexCIBContext; } }

  public List<TblNipbulkTransferLog> GetPendingTransferItems(int status, int perProcess, int tryCount)
  {
    //return _context.TblNipbulkTransferLogs.Where(ctx => ctx.TransactionStatus == status && ctx.ApprovalStatus == 1 && ctx.IntraBankStatus == 0).OrderBy(a => a.TryCount).Take(perProcess).ToList();
    return _context.TblNipbulkTransferLogs.Where(ctx =>
    ctx.TransactionStatus == status &&
    ctx.ApprovalStatus == 1 &&
    ctx.InterBankStatus == 0 &&
    ctx.ServerIp == Transactions.GetHostIp() &&
    ctx.TryCount < tryCount
     ).OrderBy(a => a.InterBankTryCount).Take(perProcess).ToList();
  }
  public List<TblNipbulkTransferLog> CheckInterBankStatus(Guid? tranId, int isPending)
  {
    return _context.TblNipbulkTransferLogs.Where(ctx => ctx.Id == tranId && ctx.ApprovalStatus == 1 && ctx.IntraBankStatus == 0).ToList();
  }
  public void UpdateStatus(TblNipbulkTransferLog status)
  {
    _context.Update(status).Property(x => x.Sn).IsModified = false;
  }
}
