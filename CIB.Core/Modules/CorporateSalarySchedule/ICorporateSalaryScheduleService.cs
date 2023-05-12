// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using CIB.Core.Entities;
// using CIB.Core.Services.Api.Dto;
// using Microsoft.Extensions.Configuration;

// namespace CIB.Core.Modules.CorporateSalarySchedule
// {
//     public interface ICorporateSalaryScheduleService
//     {
//         Task<TblNipbulkCreditLog> PrepareScheduleBeneficiary (TblCorporateSalaryScheduleBeneficiary item,string parallexBankCode,TblNipbulkTransferLog tranlg,BankListResponseData bankList,IReadOnlyList<TblFeeCharge> feeCharges);
//         List<TblNipbulkCreditLog>  PrepareBulkTransactionPosting( List<TblNipbulkCreditLog>  transactionItem, string parallexBankCode,TblNipbulkTransferLog tranlg, Guid CorporateCustomerId);
//         List<TblCorporateApprovalHistory> SetAuthorizationWorkFlow(List<TblWorkflowHierarchy> workflowHierarchies, TblNipbulkTransferLog tranlg)
//         Task<List<TblNipbulkCreditLog>> ScheduleBeneficiaries (List<TblCorporateSalaryScheduleBeneficiary> beneficiaries,TblCorporateSalarySchedule schedule, TblCorporateCustomer company,BankListResponseData bankList, IConfiguration _config,IReadOnlyList<TblFeeCharge> feeCharges);
//     }
// }