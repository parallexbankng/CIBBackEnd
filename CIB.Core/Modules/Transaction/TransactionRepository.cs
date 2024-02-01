using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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
		public IEnumerable<TblTransaction> GetCorporateTransactionReport(Guid CorporateCustomerId)
		{
			return _context.TblTransactions.Where(ctx => ctx.CorporateCustomerId == CorporateCustomerId).OrderByDescending(ctx => ctx.Sn).ToList();
		}

		public IEnumerable<TransactionReportDto> Search(Guid? corporateCustomerId, string transactionRef, DateTime dateFrom, DateTime dateTo, bool IsBulk, int pageNumber, int pageSize)
		{
			var query = IsBulk == true ? GetBulkTransactionsQuery(corporateCustomerId, transactionRef, dateFrom, dateTo, pageNumber, pageSize)
					: GetNonBulkTransactionsQuery(corporateCustomerId, transactionRef, dateFrom, dateTo, pageNumber, pageSize);

			return query;
		}

		private IEnumerable<TransactionReportDto> GetBulkTransactionsQuery(Guid? corporateCustomerId, string transactionRef, DateTime dateFrom, DateTime dateTo, int pageNumber, int pageSize)
		{
			var query = _context.TblNipbulkTransferLogs.AsQueryable();

			if (corporateCustomerId.HasValue && !string.IsNullOrEmpty(corporateCustomerId.ToString()))
			{
				query = query.Where(a => a.CompanyId == corporateCustomerId);
			}

			if (!string.IsNullOrEmpty(transactionRef))
			{
				query = query.Where(a => a.TransactionReference == transactionRef);
			}

			if (dateFrom != DateTime.MinValue && dateTo != DateTime.MinValue)
			{
				dateTo = dateTo.AddDays(1).AddMinutes(-1);
				query = query.Where(a => a.DateInitiated != null && (DateTime)a.DateInitiated >= dateFrom && (DateTime)a.DateInitiated <= dateTo);
			}

			if (dateFrom == DateTime.MinValue && dateTo == DateTime.MinValue && string.IsNullOrEmpty(transactionRef) && !corporateCustomerId.HasValue)
			{
				var items = (from trx in query
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
										 }).OrderByDescending(ctx => ctx.Date).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

				return items;
			}

			var result = (from trx in query
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
										}).OrderByDescending(ctx => ctx.Date).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

			return result;
		}

		private IEnumerable<TransactionReportDto> GetNonBulkTransactionsQuery(Guid? corporateCustomerId, string transactionRef, DateTime dateFrom, DateTime dateTo, int pageNumber, int pageSize)
		{
			var query = _context.TblNipbulkTransferLogs.AsQueryable();

			if (corporateCustomerId.HasValue && !string.IsNullOrEmpty(corporateCustomerId.ToString()))
			{
				query = query.Where(a => a.CompanyId == corporateCustomerId);
			}

			if (!string.IsNullOrEmpty(transactionRef))
			{
				query = query.Where(a => a.TransactionReference == transactionRef);
			}

			if (dateFrom != DateTime.MinValue && dateTo != DateTime.MinValue)
			{
				dateTo = dateTo.AddDays(1).AddMinutes(-1);
				query = query.Where(a => a.DateInitiated != null && (DateTime)a.DateInitiated >= dateFrom && (DateTime)a.DateInitiated <= dateTo);
			}

			var items = (from trx in query
									 join pend in _context.TblTransactions on trx.BatchId equals pend.BatchId
									 where pend.TranType != "bulk"
									 select new TransactionReportDto
									 {
										 Id = trx.Id,
										 Sn = trx.Sn,
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
									 }).OrderByDescending(ctx => ctx.Sn)
										 .Skip((pageNumber - 1) * pageSize)
										 .Take(pageSize)
										 .ToList();

			return items;
		}
	}
}