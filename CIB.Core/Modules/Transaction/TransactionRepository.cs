using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Transaction.Dto;
using CIB.Core.Utils;

namespace CIB.Core.Modules.Transaction
{
    public class TransactionRepository : Repository<TblTransaction>, ITransactionRepository
    {
        public TransactionRepository(ParallexCIBContext context) : base(context)
        {
        }
        public ParallexCIBContext context
        {
         get { return _context as ParallexCIBContext; }
        }

        // public List<TblTransaction> GetCorporateTransactions(Guid CorporateCustomerId)
        // {
        //     return _context.TblTransactions.Where(x => x.CustAuthId != null && x.CustAuthId.Value == CorporateCustomerId).ToList();
        // }

        public List<TblTransaction> GetCorporateTransactions(Guid CorporateCustomerId)
        {
            return _context.TblTransactions.Where(x => x.CorporateCustomerId == CorporateCustomerId && x.TranType.Trim().ToLower() != "bulk").OrderByDescending(ctx => ctx.Sn).ToList();
        }

        public List<TblTransaction> GetAllCorporateTransactions()
        {
            return _context.TblTransactions.OrderByDescending(ctx => ctx.Sn).ToList();
        }

        public TransactionReportDto GetTransactionReportDetail()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TransactionReportDto> Search(Guid? corporateCustomerId, string transactionRef, DateTime dateFrom, DateTime dateTo, bool IsBulk)
        {

            if(IsBulk)
            {
                var item = new List<TransactionReportDto>();
                var record = _context.Set<TblNipbulkTransferLog>().ToList();

                if (!string.IsNullOrEmpty(corporateCustomerId.ToString()))
                {
                    record = record.Where(a => a.CompanyId == corporateCustomerId).ToList();
                }

                if (!string.IsNullOrEmpty(transactionRef))
                {
                    record = record.Where(a => a.TransactionReference == transactionRef).ToList();
                }

                if(dateFrom != DateTime.MinValue && dateTo != DateTime.MinValue)
                {
                    record = record.Where(a => a.DateInitiated != null && (DateTime)a.DateInitiated.Value >= dateFrom && (DateTime)a.DateInitiated.Value <= dateTo.AddDays(1).AddMinutes(-1)).ToList();
                }

                item = (from trx in record
                join pend in _context.TblTransactions on trx.BatchId equals pend.BatchId
                where pend.TranType == "bulk"
                select new TransactionReportDto
                {
                    Id = trx.Id,
                    Status = pend.TransactionStatus,
                    Narration = trx.Narration,
                    Reference = pend.TransactionReference,
                    Amount = trx.DebitAmount,
                    DebitAccount = trx.DebitAmount.ToString(),
                    Beneficiary = trx.SuspenseAccountNumber,
                    BeneficiaryName = trx.SuspenseAccountName,
                    Sender = trx.DebitAccountNumber,
                    SenderName = trx.DebitAccountName,
                    Type = pend.TranType,
                    WorkflowId = trx.WorkflowId,
                    CreditNumber = trx.NoOfCredits,
                    Date = trx.DateInitiated
                }).ToList();
                return item.OrderByDescending(ctx=>ctx.Date);
            }


            var recordk = _context.Set<TblPendingTranLog>().OrderByDescending(ct => ct.Sn).ToList();
            var itemj = new List<TransactionReportDto>();

            if (!string.IsNullOrEmpty(corporateCustomerId.ToString()))
            {
                recordk = recordk.Where(a => a.CompanyId == corporateCustomerId).ToList();
            }

            if (!string.IsNullOrEmpty(transactionRef))
            {
                recordk = recordk.Where(a => a.TransactionReference == transactionRef).ToList();
            }

            if(dateFrom != DateTime.MinValue && dateTo != DateTime.MinValue)
            {
                recordk = recordk.Where(a => a.DateInitiated != null && (DateTime)a.DateInitiated.Value >= dateFrom && (DateTime)a.DateInitiated.Value <= dateTo.AddDays(1).AddMinutes(-1)).ToList();
            }

            itemj =  (from trx in recordk
            join pend in _context.TblTransactions on trx.BatchId equals pend.BatchId
            select new TransactionReportDto
            {
                Id = trx.Id,
                Status = pend.TransactionStatus,
                Narration = trx.Narration,
                Reference = pend.TransactionReference,
                Amount = pend.TranAmout,
                Beneficiary = pend.DestinationAcctNo,
                BeneficiaryName = pend.DestinationAcctName,
                Sender = pend.SourceAccountNo,
                SenderName = pend.SourceAccountName,
                WorkflowId = trx.WorkflowId,
                Type = pend.TranType,
                Date = pend.TranDate
            }).ToList();
            return itemj;
            
        }

        public IEnumerable<TblTransaction> GetCorporateTransactionReport(Guid CorporateCustomerId)
        {
            return _context.TblTransactions.Where(ctx => ctx.CorporateCustomerId == CorporateCustomerId).OrderByDescending(ctx => ctx.Sn).ToList();
        }
  }
}