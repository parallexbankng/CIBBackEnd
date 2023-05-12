using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.Transaction.Dto;

namespace CIB.Core.Modules.Transaction._NipbulkTransferLog
{
  public class NipBulkTransferRespository : Repository<TblNipbulkTransferLog>, INipBulkTransferLogRespository
  {
    public NipBulkTransferRespository(ParallexCIBContext context) : base(context)
    {
    }
    public ParallexCIBContext context
    {
        get { return _context as ParallexCIBContext; }
    }

    public List<TblNipbulkTransferLog> GetBulkPendingTransferLog(Guid CorporateCustomerId)
    {
      return _context.TblNipbulkTransferLogs.Where(ctx => ctx.TransactionStatus == 0 && ctx.ApprovalStatus == 0 && ctx.CompanyId == CorporateCustomerId).OrderByDescending(ctx=> ctx.Sn).ToList();
    }

    public List<TblNipbulkTransferLog> GetAuthorizedBulkTransactions(Guid CorporateCustomerId)
    {
      //return _context.TblNipbulkTransferLogs.Where(ctx => ctx.ApprovalStatus == 1 && ctx.CompanyId == CorporateCustomerId).OrderByDescending(ctx=> ctx.Sn).ToList();
      var itemsList = from item in _context.TblNipbulkTransferLogs.Where(ctx => ctx.ApprovalStatus == 1 && ctx.CompanyId == CorporateCustomerId)
        select new TblNipbulkTransferLog
        {
          Id = item.Id,
          Sn = item.Sn,
          CompanyId = item.CompanyId,
          InitiatorId = item.InitiatorId,
          BatchId = item.BatchId,
          DebitAccountNumber = item.DebitAccountNumber,
          DebitAccountName = item.DebitAccountName,
          DebitAmount = item.DebitAmount,
          DateInitiated = item.DateInitiated,
          DateProccessed = item.DateProccessed,
          ApprovalStatus = item.ApprovalStatus,
          PostingType = item.PostingType,
          Status = item.Status,
          NoOfCredits = item.NoOfCredits,
          TransactionStatus = item.TransactionStatus,
          TransferType = item.TransferType,
          ApprovalStage = item.ApprovalStage,
          Currency = item.Currency,
          Narration = item.Narration,
          OriginatorBvn = item.OriginatorBvn,
          ApprovalCount = item.ApprovalCount,
          TryCount = item.TryCount,
          TransactionLocation = item.TransactionLocation,
          SuspenseAccountNumber = item.SuspenseAccountNumber,
          SuspenseAccountName = item.SuspenseAccountName,
          InitiatorUserName = item.InitiatorUserName,
          TransactionReference = item.TransactionReference,
          WorkflowId = item.WorkflowId,
          TotalCredits = (item.TotalCredits + item.InterBankTotalCredits),
        };
      return itemsList.OrderByDescending(ctx => ctx.Sn).ToList();
    }

    public void UpdatebulkTransfer(TblNipbulkTransferLog transferLog)
    {
      _context.Update(transferLog).Property(x => x.Sn).IsModified = false;
    }

    public List<TblNipbulkTransferLog> GetCompanyBulkTransactions(Guid CorporateCustomerId)
    {
      return _context.TblNipbulkTransferLogs.Where(ctx => ctx.CompanyId == CorporateCustomerId).OrderByDescending(ctx=> ctx.Sn).ToList();
    }

    public List<TblNipbulkTransferLog> GetAllDeclineTransaction(Guid CorporateCustomerId)
    {
      return _context.TblNipbulkTransferLogs.Where(ctx => ctx.CompanyId == CorporateCustomerId && ctx.Status == (int)ProfileStatus.Declined).OrderByDescending(ctx=> ctx.Sn).ToList();
    }
  }
}
