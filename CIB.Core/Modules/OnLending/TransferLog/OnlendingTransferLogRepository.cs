using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using CIB.Core.Modules.OnLending.TransferLog.Dto;
using Microsoft.EntityFrameworkCore;

namespace CIB.Core.Modules.OnLending.TransferLog
{
  public class OnlendingTransferLogRepository : Repository<TblOnlendingTransferLog>, IOnlendingTransferLogRepository
  {
    public OnlendingTransferLogRepository(ParallexCIBContext context) : base(context)
    {
    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public async Task<IEnumerable<TblOnlendingTransferLog>> GetAllOnlendingBatches(Guid corporateCustomerId)
    {
      return await _context.TblOnlendingTransferLogs.Where(ext => ext.CorporateCustomerId != null && ext.CorporateCustomerId == corporateCustomerId).ToListAsync();
    }

    public void UpdateOnlendingTransferLog(TblOnlendingTransferLog update)
    {
      _context.Update(update).Property(x => x.Sn).IsModified = false;
    }

    public async Task<IEnumerable<BeneficiaryDto>> GetOnlendingBatchBeneficiaryBatchId(Guid batchId)
    {
      var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.BatchId == batchId)
                       join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
                       select new BeneficiaryDto
                       {
                         Id = beneficiary.Id,
                         Title = beneficiary.Title,
                         FirstName = beneficiary.FirstName,
                         SurName = beneficiary.SurName,
                         MiddleName = beneficiary.MiddleName,
                         PhoneNo = beneficiary.PhoneNo,
                         Email = beneficiary.Email,
                         Gender = beneficiary.Gender,
                         StreetNo = beneficiary.StreetNo,
                         Address = beneficiary.Address,
                         City = beneficiary.City,
                         State = beneficiary.State,
                         Lga = beneficiary.Lga,
                         DateOfBirth = DateTime.Parse(beneficiary.DateOfBirth.ToString()).ToString("dd-MMM-yyyy"),
                         Bvn = beneficiary.Bvn,
                         AccountNumber = beneficiary.AccountName,
                         DocType = beneficiary.DocType,
                         MaritalStatus = beneficiary.MaritalStatus,
                         IdNumber = beneficiary.IdNumber,
                         Nationality = beneficiary.Nationality,
                         DateIssued = DateTime.Parse(beneficiary.IdIssuedDate.ToString()).ToString("dd-MMM-yyyy"),
                         StateOfResidence = beneficiary.StateOfResidence,
                         PlaceOfBirth = beneficiary.PlaceOfBirth,
                         Region = beneficiary.Region,
                         FundAmount = creditLog.FundAmount,
                         PreferredNarration = creditLog.Narration,
                         RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
                         Error = creditLog.Error
                       }).ToListAsync();
      return await batchInfo;
    }

    public async Task<IEnumerable<BeneficiaryDto>> GetOnlendingValidBatchByBatchId(Guid batchId)
    {
      var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.VerificationStatus == 1 && ctx.BatchId == batchId)
                       join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
                       select new BeneficiaryDto
                       {
                         Id = beneficiary.Id,
                         Title = beneficiary.Title,
                         FirstName = beneficiary.FirstName,
                         SurName = beneficiary.SurName,
                         MiddleName = beneficiary.MiddleName,
                         PhoneNo = beneficiary.PhoneNo,
                         Email = beneficiary.Email,
                         Gender = beneficiary.Gender,
                         StreetNo = beneficiary.StreetNo,
                         Address = beneficiary.Address,
                         City = beneficiary.City,
                         State = beneficiary.State,
                         Lga = beneficiary.Lga,
                         DateOfBirth = DateTime.Parse(beneficiary.DateOfBirth.ToString()).ToString("dd-MMM-yyyy"),
                         Bvn = beneficiary.Bvn,
                         AccountNumber = beneficiary.AccountNumber,
                         DocType = beneficiary.DocType,
                         MaritalStatus = beneficiary.MaritalStatus,
                         IdNumber = beneficiary.IdNumber,
                         Nationality = beneficiary.Nationality,
                         DateIssued = DateTime.Parse(beneficiary.IdIssuedDate.ToString()).ToString("dd-MMM-yyyy"),
                         StateOfResidence = beneficiary.StateOfResidence,
                         PlaceOfBirth = beneficiary.PlaceOfBirth,
                         Region = beneficiary.Region,
                         FundAmount = creditLog.FundAmount,
                         PreferredNarration = creditLog.Narration,
                         RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
                       }).ToListAsync();
      return await batchInfo;
    }

    public async Task<IEnumerable<BeneficiaryDto>> GetOnlendingInvalidBatchByBatchId(Guid batchId)
    {
      var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.VerificationStatus == 2 && ctx.BatchId == batchId)
                       join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
                       select new BeneficiaryDto
                       {
                         Id = beneficiary.Id,
                         Title = beneficiary.Title,
                         FirstName = beneficiary.FirstName,
                         SurName = beneficiary.SurName,
                         MiddleName = beneficiary.MiddleName,
                         PhoneNo = beneficiary.PhoneNo,
                         Email = beneficiary.Email,
                         Gender = beneficiary.Gender,
                         StreetNo = beneficiary.StreetNo,
                         Address = beneficiary.Address,
                         City = beneficiary.City,
                         State = beneficiary.State,
                         Lga = beneficiary.Lga,
                         DateOfBirth = DateTime.Parse(beneficiary.DateOfBirth.ToString()).ToString("dd-MMM-yyyy"),
                         Bvn = beneficiary.Bvn,
                         AccountNumber = beneficiary.AccountName,
                         DocType = beneficiary.DocType,
                         MaritalStatus = beneficiary.MaritalStatus,
                         IdNumber = beneficiary.IdNumber,
                         Nationality = beneficiary.Nationality,
                         DateIssued = DateTime.Parse(beneficiary.IdIssuedDate.ToString()).ToString("dd-MMM-yyyy"),
                         StateOfResidence = beneficiary.StateOfResidence,
                         PlaceOfBirth = beneficiary.PlaceOfBirth,
                         Region = beneficiary.Region,
                         FundAmount = creditLog.FundAmount,
                         PreferredNarration = creditLog.Narration,
                         RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
                         Error = creditLog.Error,
                       }).ToListAsync();
      return await batchInfo;
    }

    public async Task<IEnumerable<ReportListResponse>> GetOnlendingCorporateValidBatchBeneficiary(Guid batch)
    {
      var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.BatchId != null && ctx.BatchId == batch && ctx.VerificationStatus == 1)
                       join transferLog in _context.TblOnlendingTransferLogs on creditLog.BatchId equals transferLog.BatchId
                       join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
                       select new ReportListResponse
                       {
                         Id = transferLog.Id,
                         BeneficiaryName = $"{beneficiary.SurName} {beneficiary.FirstName}",
                         Amount = creditLog.FundAmount,
                         SourceAccountNumber = transferLog.DebitAccountNumber,
                         RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
                         ContractStatus = transferLog.Status,
                       }).ToListAsync();
      return await batchInfo;
    }

    public async Task<IEnumerable<TblOnlendingTransferLog>> GetOnlendingTransferLogByBatchId(Guid batchId)
    {
      return await _context.TblOnlendingTransferLogs.Where(ctx => ctx.BatchId == batchId).ToListAsync();
    }

    public async Task<TblOnlendingTransferLog> GetAllOnlendingByBatchId(Guid batchId)
    {
      return await _context.TblOnlendingTransferLogs.Where(ctx => ctx.BatchId == batchId).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ReportResponse>> GetOnlendingCorporateValidBatch(Guid corporateCustomerId)
    {
      var batchInfo = (from transferLog in _context.TblOnlendingTransferLogs.Where(ctx => ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == corporateCustomerId)

                       select new ReportResponse
                       {
                         Id = transferLog.Id,
                         Amount = transferLog.TotalValidAmount,
                         BatchId = transferLog.BatchId,
                         TotalPayment = transferLog.ValidCount,
                         InitiatedBy = transferLog.InitiatorUserName,
                         PaymentType = transferLog.PostingType,
                         SourceAccountNumber = transferLog.DebitAccountNumber,
                         Date = DateTime.Parse(transferLog.DateInitiated.ToString()).ToString("dd-MMM-yyyy"),
                       }).ToListAsync();
      return await batchInfo;
    }
		

		public async Task<TblOnlendingTransferLog> GetOnlendingByBatchId(Guid batchId)
    {
      return await _context.TblOnlendingTransferLogs.Where(ctx => ctx.BatchId == batchId).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BatchBeneficaryResponse>> GetValidBatchBeneficiariesByBatchId(Guid batchId)
    {
      var batchInfo = (from creditLog in _context.TblOnlendingCreditLogs.Where(ctx => ctx.BatchId != null && ctx.BatchId == batchId && ctx.VerificationStatus == 1)
                       join transferLog in _context.TblOnlendingTransferLogs on creditLog.BatchId equals transferLog.BatchId
                       join beneficiary in _context.TblOnlendingBeneficiaries on creditLog.BeneficiaryId equals beneficiary.Id
                       select new BatchBeneficaryResponse
                       {
                         Id = creditLog.Id,
                         BatchId = transferLog.BatchId,
                         BeneficiaryName = $"{beneficiary.SurName} {beneficiary.FirstName}",
                         Amount = creditLog.FundAmount,
                         BeneficiaryAccountNumber = beneficiary.AccountNumber != null ? beneficiary.AccountNumber : "------------------",
                         RepaymentDate = DateTime.Parse(creditLog.RepaymentDate.ToString()).ToString("dd-MMM-yyyy"),
                         Narration = creditLog.Narration,
                       }).ToListAsync();
      return await batchInfo;
    }

		public async Task<IEnumerable<TblOnlendingTransferLog>> GetPendingOnlendingTransferLogList(Guid batchId)
    {
      return await _context.TblOnlendingTransferLogs.Where(ctx => ctx.BatchId == batchId && ctx.ApprovalStatus == 0).ToListAsync();
    }

    public async Task<TblOnlendingTransferLog> GetPendingOnlendingTransferLog(Guid batchId)
    {
      return await _context.TblOnlendingTransferLogs.Where(ctx => ctx.BatchId == batchId && ctx.ApprovalStatus == 0).FirstOrDefaultAsync();
    }
  }
}