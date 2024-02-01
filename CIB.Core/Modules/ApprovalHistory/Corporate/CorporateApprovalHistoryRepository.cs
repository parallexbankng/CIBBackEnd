using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;

namespace CIB.Core.Modules.ApprovalHistory.Corporate
{
	public class CorporateApprovalHistoryRepository : Repository<TblCorporateApprovalHistory>, ICorporateApprovalHistoryRepository
	{
		public CorporateApprovalHistoryRepository(ParallexCIBContext context) : base(context)
		{
		}
		public ParallexCIBContext context
		{
			get { return _context as ParallexCIBContext; }
		}

		public TblCorporateApprovalHistory GetCorporateAuthorizationHistoryByAuthId(Guid authId, Guid transLogId)
		{
			return _context.TblCorporateApprovalHistories.Where(a => a.UserId == authId && a.LogId == transLogId).SingleOrDefault
				();
		}

		public TblCorporateApprovalHistory GetCorporateOnlendingAuthorizationHistoryByAuthId(Guid authId, Guid transLogId)
		{
			return _context.TblCorporateApprovalHistories.Where(a => a.UserId == authId && a.LogId == transLogId && a.Status == nameof(AuthorizationStatus.Pending)).SingleOrDefault();
		}

		// public TblCorporateApprovalHistory GetCorporateAuthorizationHistoryByAuthId(Guid authId, Guid transLogId, string action)
		// {
		//    return _context.TblCorporateApprovalHistories.Where(a => a.UserId == authId && a.LogId == transLogId).SingleOrDefault();
		// }

		public List<TblCorporateApprovalHistory> GetCorporateAuthorizationHistoryPendingTrandLogId(Guid transLogId, Guid CorporateCustomerId)
		{
			return _context.TblCorporateApprovalHistories.Where(a => a.CorporateCustomerId == CorporateCustomerId && a.LogId == transLogId).OrderByDescending(ctx => ctx.Sn).ToList();
		}

		public List<TblCorporateApprovalHistory> GetCorporateBulkAuthorizationHistories(Guid transLogId)
		{
			return _context.TblCorporateApprovalHistories.Where(a => a.LogId == transLogId).OrderByDescending(ctx => ctx.Sn).ToList();
		}

		public void UpdateCorporateApprovalHistory(TblCorporateApprovalHistory update)
		{
			_context.Update(update).Property(x => x.Sn).IsModified = false;
		}

		public TblCorporateApprovalHistory GetNextApproval(TblPendingTranLog tranLog)
		{
			return _context.TblCorporateApprovalHistories.FirstOrDefault(ctx => ctx.ApprovalLevel == tranLog.ApprovalStage && ctx.ToApproved == 0 && ctx.LogId == tranLog.Id && ctx.Status == "Pending");
		}

		public TblCorporateApprovalHistory GetNextBulkApproval(TblNipbulkTransferLog tranLog)
		{
			return _context.TblCorporateApprovalHistories.FirstOrDefault(ctx => ctx.ApprovalLevel == tranLog.ApprovalStage && ctx.ToApproved == 0 && ctx.LogId == tranLog.Id && ctx.Status == "Pending");
		}



	}
}

