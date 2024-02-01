using System;
using System.Collections.Generic;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Transaction._PendingCreditLog.Dto;

namespace CIB.Core.Modules.Transaction._PendingCreditLog
{
	public interface IPendingCreditLogRepository : IRepository<TblPendingCreditLog>
	{
		TblPendingCreditLog GetPendingCreditTranLogByTranLogId(Guid TranLogId);
		List<TblPendingCreditLog> GetPendingCreditTranLogsByTranLogId(Guid tranLogId);
		List<SingleTransactionDto> GetCompanyCreditTranLogs(Guid CorporateCustomerId);
		SingleTransactionDto GetCreditTranLog(Guid tranLogId);

		void UpdatePendingCreditLog(TblPendingCreditLog update);
	}
}
