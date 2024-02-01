using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;

namespace CIB.Core.Modules.ApprovalHistory.Corporate
{
	public interface ICorporateApprovalHistoryRepository : IRepository<TblCorporateApprovalHistory>
	{
		TblCorporateApprovalHistory GetCorporateAuthorizationHistoryByAuthId(Guid authId, Guid transLogId);
		List<TblCorporateApprovalHistory> GetCorporateAuthorizationHistoryPendingTrandLogId(Guid transLogId, Guid CorporateCustomerId);
		List<TblCorporateApprovalHistory> GetCorporateBulkAuthorizationHistories(Guid transLogId);
		void UpdateCorporateApprovalHistory(TblCorporateApprovalHistory update);
		TblCorporateApprovalHistory GetNextApproval(TblPendingTranLog tranLog);
		TblCorporateApprovalHistory GetNextBulkApproval(TblNipbulkTransferLog tranLog);
		TblCorporateApprovalHistory GetCorporateOnlendingAuthorizationHistoryByAuthId(Guid authId, Guid transLogId);
	}
}