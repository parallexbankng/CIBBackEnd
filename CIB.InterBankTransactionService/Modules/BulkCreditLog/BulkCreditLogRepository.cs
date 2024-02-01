
using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.Common.Repository;

namespace CIB.InterBankTransactionService.Modules.BulkCreditLog;

  public class BulkCreditLogRepository : Repository<TblNipbulkCreditLog>,IBulkCreditLogRepository
  {
      public BulkCreditLogRepository(ParallexCIBContext context) : base(context)
      {
      }

      public ParallexCIBContext Context { get { return _context as ParallexCIBContext; }}
      public List<TblNipbulkCreditLog> GetPendingCredit(Guid tranLogId, int status, string bankCode, DateTime processDate)
      {
        return _context.TblNipbulkCreditLogs.Where(ctx => 
        ctx.InitiateDate != null && 
        ctx.TranLogId == tranLogId && 
        ctx.CreditStatus == status && 
        ctx.NameEnquiryStatus == 1 && 
        ctx.CreditBankCode != bankCode &&
        ctx.InitiateDate.Value.Date == processDate).ToList();
      }
      public List<TblNipbulkCreditLog> CheckForPendingCredit(Guid tranLogId, int status, DateTime processDate)
      {
        return _context.TblNipbulkCreditLogs.Where(ctx => ctx.InitiateDate != null && ctx.TranLogId == tranLogId && ctx.CreditStatus != 1 && ctx.NameEnquiryStatus == 1 && ctx.InitiateDate.Value.Date == processDate).ToList();
      }

      public int GetInterBankTotalCredit(Guid tranLogId, string bankCode, DateTime processDate)
      {
        return _context.TblNipbulkCreditLogs.Count(ctx => 
          ctx.InitiateDate != null && 
          ctx.TranLogId == tranLogId && 
          ctx.CreditStatus == 1 && 
          ctx.NameEnquiryStatus == 1 && 
          ctx.InitiateDate.Value.Date == processDate);
      }

      public void UpdateCreditStatus(TblNipbulkCreditLog status)
      {
        _context.Update(status).Property(x=>x.Sn).IsModified = false;
      }
}
