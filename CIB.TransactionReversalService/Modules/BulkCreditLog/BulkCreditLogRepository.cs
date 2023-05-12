
using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common.Repository;

namespace CIB.TransactionReversalService.Modules.BulkCreditLog;

  public class BulkCreditLogRepository : Repository<TblNipbulkCreditLog>,IBulkCreditLogRepository
  {
      public BulkCreditLogRepository(ParallexCIBContext context) : base(context)
      {
      }
      public new ParallexCIBContext Context { get { return base.Context as ParallexCIBContext; }}
      public List<TblNipbulkCreditLog> GetFailedTransaction(int status, int retryCount, int totalPerProcess, DateTime processDate)
      {
        return base.Context.TblNipbulkCreditLogs.Where(ctx => 
        ctx.CreditDate != null && 
        ctx.CreditStatus == status && 
        ctx.NameEnquiryStatus == 1 && 
        ctx.TryCount == retryCount && 
        ctx.CreditDate.Value.Date == processDate
        ).OrderBy(a => a.TryCount).Take(totalPerProcess).ToList();
      }

  public List<TblNipbulkCreditLog> GetFailedTransaction(Guid tranId, int status, int retryCount, int totalPerProcess, DateTime processDate)
  {
    return base.Context.TblNipbulkCreditLogs.Where(ctx => 
    ctx.TranLogId == tranId &&
        ctx.CreditDate != null && 
        ctx.CreditStatus == status && 
        ctx.NameEnquiryStatus == 1 && 
        ctx.CreditDate.Value.Date == processDate
        ).Take(totalPerProcess).ToList();
  }

  public void UpdateCreditStatus(TblNipbulkCreditLog status)
      {
          base.Context.Update(status).Property(x=>x.Sn).IsModified = false;
      }
}
