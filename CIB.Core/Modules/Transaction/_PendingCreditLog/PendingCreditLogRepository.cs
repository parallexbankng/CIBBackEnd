
using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Transaction._PendingCreditLog.Dto;

namespace CIB.Core.Modules.Transaction._PendingCreditLog
{
  public class PendingCreditLogRepository : Repository<TblPendingCreditLog>,IPendingCreditLogRepository
  {
    public PendingCreditLogRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public List<TblPendingCreditLog> GetPendingCreditTranLogsByTranLogId(Guid tranLogId)
    {
      return _context.TblPendingCreditLogs.Where(a => a.TranLogId == tranLogId && a.CreditStatus == 0).OrderByDescending(ctx => ctx.Sn).ToList();
    }

    public List<TblPendingCreditLog> GetPendingCreditTranLogs()
    {
      return _context.TblPendingCreditLogs.Where(a => a.CreditStatus != 0).ToList();
    }

    public TblPendingCreditLog GetPendingCreditTranLogByTranLogId(Guid TranLogId)
    {
      return _context.TblPendingCreditLogs.FirstOrDefault(a => a.TranLogId == TranLogId);
    }

    public void UpdatePendingCreditLog(TblPendingCreditLog update)
    {
      _context.Update(update).Property(x => x.Sn).IsModified = false;
    }

    public List<SingleTransactionDto> GetCompanyCreditTranLogs(Guid CorporateCustomerId)
    {
      var record = _context.TblPendingTranLogs.Where(a => a.CompanyId != null && a.CompanyId == CorporateCustomerId && a.Status != 0).ToList();
      var item = (from trx in record
        join pend in _context.TblPendingCreditLogs on trx.Id equals pend.TranLogId
        where pend.CorporateCustomerId == CorporateCustomerId
        select new SingleTransactionDto
        {
          Id = pend.Id,
          Sn = pend.Sn,
          CustAuthId = trx.InitiatorId,
          TranAmout = pend.CreditAmount,
          SourceAccountNo = trx.DebitAccountNumber,
          SourceAccountName = trx.DebitAccountName,
          SourceBank = "",
          TranDate = pend.CreditDate,
          TranType = trx.TransferType,
          Narration = trx.Narration,
          DestinationAcctNo = pend.CreditAccountNumber,
          DestinationAcctName = pend.CreditAccountName,
          DesctionationBank= pend.CreditBankCode,
          Channel = pend.ChannelCode,
          TransactionReference = pend.TransactionReference,
          BatchId = trx.BatchId,
          TransactionStatus =  trx.Status == 1 ? "Successful" : trx.Status == 3 ? "Decline" : "Failed",
          CorporateCustomerId = pend.CorporateCustomerId
        }).ToList();
      return item;
    }
  }
}
