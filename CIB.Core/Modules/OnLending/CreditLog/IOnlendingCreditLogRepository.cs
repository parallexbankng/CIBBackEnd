
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using CIB.Core.Modules.OnLending.CreditLog.Dto;
using CIB.Core.Modules.OnLending.TransferLog.Dto;

namespace CIB.Core.Modules.OnLending.CreditLog
{
	public interface IOnlendingCreditLogRepository : IRepository<TblOnlendingCreditLog>
	{
		bool CheckForDoubleOnlendingRequestByBVN(string bvn);
		void UpdateOnlendingCreditLog(TblOnlendingCreditLog update);
		Task<IEnumerable<TblOnlendingCreditLog>> GetOnlendingBeneficiaryByTranLogId(Guid tranLog);
		Task<TblOnlendingCreditLog> GetOnlendingCreditLogById(Guid id);
		Task<IEnumerable<BeneficiaryId>> GetOnlendingBeneficiaryIdsByTranLogId(Guid tranLog);
		Task<IEnumerable<TblOnlendingCreditLog>> GetOnlendingBeneficiaryCreditLogStatus(Guid tranLog, int creditStatus);
		Task<IEnumerable<BatchBeneficaryResponse>> GetOnlendingPreliquidateBeneficiaries(Guid batchId);
		Task<IEnumerable<BatchBeneficaryResponse>> GetOnlendingRepaymentExtensionRequestBeneficiaries(Guid batchId);
		void UpdateOnlendingCreditLogList(IEnumerable<TblOnlendingCreditLog> update);
	}
}