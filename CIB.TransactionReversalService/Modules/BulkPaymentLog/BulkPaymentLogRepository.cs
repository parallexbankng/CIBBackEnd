
using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common.Repository;

namespace CIB.TransactionReversalService.Modules.BulkPaymentLog;

  public class BulkPaymentLogRepository : Repository<TblNipbulkTransferLog>,IBulkPaymentLogRepository
  {
    public BulkPaymentLogRepository(ParallexCIBContext context) : base(context)
    {
    }
    //public new ParallexCIBContext Context { get { return base.Context as ParallexCIBContext; }}
     public ParallexCIBContext Context { get { return _context as ParallexCIBContext; }}


    public List<TblNipbulkTransferLog> GetPendingTransferItems(int status,int perProcess, int TryCount, DateTime proccessDate)
    {
      return _context.TblNipbulkTransferLogs.Where(ctx => 
      ctx.TransactionStatus == status && 
      ctx.ApprovalStatus == 1 && 
      ctx.DateProccessed.Value.Date == proccessDate &&
      ctx.TryCount == TryCount).OrderBy(a => a.TryCount).Take(perProcess).ToList();
    }

    public AccountInfo GetAccountInfo(Guid tranId)
    {
      var record = _context.TblNipbulkTransferLogs.FirstOrDefault(ctx => ctx.Id == tranId);
      return new AccountInfo{  
        SuspenseAccountName= record?.SuspenseAccountName,
        SuspenseAccountNumber = record?.SuspenseAccountNumber,  
        SourceAccountNumber= record?.DebitAccountNumber,
        SourceAccountName = record?.DebitAccountName,
        InterBankSuspenseAccountName = record?.IntreBankSuspenseAccountName,
        InterBankSuspenseAccountNumber = record?.IntreBankSuspenseAccountNumber,
        TransactionLocation = record?.TransactionLocation,
        UserName = record?.InitiatorUserName
      };  
      
    }

    public List<TblNipbulkTransferLog> CheckInterBankStatus(Guid? tranId, int isPending)
    {
      return _context.TblNipbulkTransferLogs.Where(ctx => ctx.Id == tranId && ctx.ApprovalStatus == 1 && ctx.IntraBankStatus == 0).ToList();
    }
    public void UpdateStatus(TblNipbulkTransferLog status)
    {
      _context.Update(status).Property(x=>x.Sn).IsModified = false;
    }

  public List<TblNipbulkTransferLog> GetPendingTransferItems(int status, int perProcess)
  {
    throw new NotImplementedException();
  }
}
