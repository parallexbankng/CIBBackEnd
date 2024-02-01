
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Modules.Common.Repository;

namespace CIB.IntraBankTransactionService.Modules.BulkCreditLog;

public class BulkCreditLogRepository : Repository<TblNipbulkCreditLog>, IBulkCreditLogRepository
{
  public BulkCreditLogRepository(ParallexCIBContext context) : base(context)
  {
  }
  public ParallexCIBContext Context { get { return _context as ParallexCIBContext; } }

  public List<TblNipbulkCreditLog> GetPendingCredit(Guid tranLogId, int status, string bankCode, DateTime processDate)
  {
    return _context.TblNipbulkCreditLogs.Where(ctx => ctx.TranLogId == tranLogId && ctx.CreditStatus == status && ctx.NameEnquiryStatus == 1 && ctx.CreditBankCode == bankCode && ctx.TryCount <= 5).ToList();
  }

  public List<TblNipbulkCreditLog> CheckForPendingCredit(Guid tranLogId, int status, DateTime processDate)
  {
    return _context.TblNipbulkCreditLogs.Where(ctx => ctx.InitiateDate != null && ctx.TranLogId == tranLogId && ctx.CreditStatus != 0 && ctx.NameEnquiryStatus == 1 && ctx.InitiateDate.Value.Date == processDate).ToList();
  }

  public void UpdateCreditStatus(TblNipbulkCreditLog status)
  {
    _context.Update(status).Property(x => x.Sn).IsModified = false;
  }
}
