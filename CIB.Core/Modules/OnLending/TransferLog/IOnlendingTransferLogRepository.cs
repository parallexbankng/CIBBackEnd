using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using CIB.Core.Modules.OnLending.TransferLog.Dto;

namespace CIB.Core.Modules.OnLending.TransferLog
{
	public interface IOnlendingTransferLogRepository : IRepository<TblOnlendingTransferLog>
	{

		Task<IEnumerable<BeneficiaryDto>> GetOnlendingBatchBeneficiaryBatchId(Guid batchId);
		Task<IEnumerable<BeneficiaryDto>> GetOnlendingInvalidBatchByBatchId(Guid batchId);
		Task<IEnumerable<BeneficiaryDto>> GetOnlendingValidBatchByBatchId(Guid batchId);
		Task<IEnumerable<ReportResponse>> GetOnlendingCorporateValidBatch(Guid corporateCustomerId);
		Task<IEnumerable<TblOnlendingTransferLog>> GetAllOnlendingBatches(Guid corporateCustomerId);
		Task<IEnumerable<ReportListResponse>> GetOnlendingCorporateValidBatchBeneficiary(Guid batchId);
		Task<IEnumerable<BatchBeneficaryResponse>> GetValidBatchBeneficiariesByBatchId(Guid batchId);
		Task<TblOnlendingTransferLog> GetOnlendingByBatchId(Guid batchId);
		Task<IEnumerable<TblOnlendingTransferLog>> GetPendingOnlendingTransferLogList(Guid batchId);
		Task<TblOnlendingTransferLog> GetPendingOnlendingTransferLog(Guid batchId);
		void UpdateOnlendingTransferLog(TblOnlendingTransferLog update);
	}
}