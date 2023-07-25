using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.OnLending.Enums;
using CIB.Core.Modules.OnLending.TransferLog.Dto;
using Microsoft.EntityFrameworkCore;

namespace CIB.Core.Modules.OnLending.CreditLog
{
	public class OnlendingCreditLogRepository : Repository<TblOnlendingCreditLog>, IOnlendingCreditLogRepository
	{
		public OnlendingCreditLogRepository(ParallexCIBContext context) : base(context)
		{
		}
		public ParallexCIBContext context
		{
			get { return _context as ParallexCIBContext; }
		}

		public bool CheckForDoubleOnlendingRequestByBVN(string bvn)
		{
			var getBeneficiary = _context.TblOnlendingBeneficiaries.Where(ctx => ctx.Bvn != null && ctx.Bvn.Trim() == bvn.Trim()).FirstOrDefault();
			var status = getBeneficiary != null ? _context.TblOnlendingCreditLogs.Where(ctx => ctx.BeneficiaryId != null && ctx.BeneficiaryId == getBeneficiary.Id && ctx.Status == 1).FirstOrDefault() : null;
			return status != null;
		}

		public void UpdateOnlendingCreditLog(TblOnlendingCreditLog update)
		{
			_context.Update(update).Property(x => x.Sn).IsModified = false;
		}

		public async Task<IEnumerable<TblOnlendingCreditLog>> GetOnlendingBeneficiaryByTranLogId(Guid tranLog)
		{
			return await (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.TranLogId != null && ctx.TranLogId.Value == tranLog && ctx.VerificationStatus == 1)
										join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
										select new TblOnlendingCreditLog
										{
											Id = creditLog.Id,
											AccountNumber = beneficiary.AccountNumber,
											FundAmount = creditLog.FundAmount,
											RepaymentDate = creditLog.RepaymentDate,
											Narration = creditLog.Narration,
										}).ToListAsync();
		}

		public async Task<IEnumerable<BeneficiaryId>> GetOnlendingBeneficiaryIdsByTranLogId(Guid tranLog)
		{
			return await (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.TranLogId != null && ctx.TranLogId.Value == tranLog && ctx.VerificationStatus == 1)
										select new BeneficiaryId
										{
											Id = creditLog.Id,
										}).ToListAsync();
		}

		public async Task<IEnumerable<TblOnlendingCreditLog>> GetOnlendingBeneficiaryCreditLogStatus(Guid tranLog, int creditStatus)
		{
			return await (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.TranLogId != null && ctx.TranLogId.Value == tranLog && ctx.VerificationStatus == 1 && ctx.CreditStatus == creditStatus)
										join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
										select new TblOnlendingCreditLog
										{
											Id = creditLog.Id,
											AccountNumber = beneficiary.AccountNumber,
											FundAmount = creditLog.FundAmount,
											RepaymentDate = creditLog.RepaymentDate,
											Narration = creditLog.Narration,
										}).ToListAsync();
		}

		public void UpdateOnlendingCreditLogList(IEnumerable<TblOnlendingCreditLog> update)
		{
			foreach (var item in update)
			{
				_context.Update(item).Property(x => x.Sn).IsModified = false;
			}
		}

		public async Task<TblOnlendingCreditLog> GetOnlendingCreditLogById(Guid id)
		{
			return await (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.Id == id)
										join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
										select new TblOnlendingCreditLog
										{
											Id = creditLog.Id,
											AccountNumber = beneficiary.AccountNumber,
											FundAmount = creditLog.FundAmount,
											RepaymentDate = creditLog.RepaymentDate,
											Narration = creditLog.Narration,
										}).FirstOrDefaultAsync();
		}

		public async Task<IEnumerable<BatchBeneficaryResponse>> GetOnlendingRepaymentExtensionRequestBeneficiaries(Guid batchId)
		{
			var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.BatchId != null && ctx.BatchId == batchId && ctx.VerificationStatus == 1 && ctx.Status == (int)OnlendingStatus.Extended)
											 join transferLog in _context.TblOnlendingTransferLogs on creditLog.BatchId equals transferLog.BatchId
											 join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
											 select new BatchBeneficaryResponse
											 {
												 Id = creditLog.Id,
												 BatchId = transferLog.BatchId,
												 BeneficiaryName = $"{beneficiary.SurName} {beneficiary.FirstName}",
												 Amount = creditLog.FundAmount,
												 BeneficiaryAccountNumber = beneficiary.AccountNumber,
												 RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
												 Narration = creditLog.Narration,
											 }).ToListAsync();
			return await batchInfo;
		}

		public async Task<IEnumerable<BatchBeneficaryResponse>> GetOnlendingPreliquidateBeneficiaries(Guid batchId)
		{
			var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.BatchId != null && ctx.BatchId == batchId && ctx.VerificationStatus == 1 && ctx.Status == (int)OnlendingStatus.PartialLiquidation)
											 join transferLog in _context.TblOnlendingTransferLogs on creditLog.BatchId equals transferLog.BatchId
											 join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
											 select new BatchBeneficaryResponse
											 {
												 Id = creditLog.Id,
												 BatchId = transferLog.BatchId,
												 BeneficiaryName = $"{beneficiary.SurName} {beneficiary.FirstName}",
												 Amount = creditLog.FundAmount,
												 BeneficiaryAccountNumber = beneficiary.AccountNumber,
												 RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
												 Narration = creditLog.Narration,
											 }).ToListAsync();
			return await batchInfo;
		}

		
	}
}