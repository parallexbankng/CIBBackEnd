// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using CIB.Core.Common;
// using CIB.Core.Common.Interface;
// using CIB.Core.Entities;
// using CIB.Core.Enums;
// using CIB.Core.Modules.CorporateSalarySchedule.Dto;
// using CIB.Core.Modules.Transaction.Dto;
// using CIB.Core.Modules.Transaction.Dto.Intrabank;
// using CIB.Core.Services.Api.Dto;
// using CIB.Core.Templates;
// using CIB.Core.Utils;
// using Microsoft.Extensions.Configuration;

// namespace CIB.Core.Modules.CorporateSalarySchedule
// {
//     public class CorporateSalaryScheduleService : ICorporateSalaryScheduleService
//     {
//         private readonly IUnitOfWork _unitOfWork;
//         public CorporateSalaryScheduleService(IUnitOfWork unitOfWork)
//         {
//             this._unitOfWork = unitOfWork;
//         }

//         private static List<TblNipbulkCreditLog>  PrepareBulkTransactionPosting( List<TblNipbulkCreditLog>  transactionItem, string parallexBankCode,TblNipbulkTransferLog tranlg, Guid CorporateCustomerId)
//         {
//             var interBankCreditItems = transactionItem.Where(ctx => ctx.CreditBankCode != parallexBankCode);
//             var intraBankCreditItems = transactionItem.Where(ctx => ctx.CreditBankCode == parallexBankCode);
//             var totalDebitAmountWithOutCharges = transactionItem.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
//             var interBankTotalDebitAmount = interBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
//             var intraBankTotalDebitAmount = intraBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
//             var bulkSuspenseCreditItems = new List<TblNipbulkCreditLog>();           
//             if (interBankCreditItems.Any())
//             {
//                 var totalVat = interBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.Vat);
//                 var totalFee = interBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.Fee);
//                 bulkSuspenseCreditItems.AddRange(new [] {
//                     new TblNipbulkCreditLog{
//                         Id = Guid.NewGuid(),
//                         TranLogId = tranlg.Id,
//                         CreditAccountNumber = tranlg.IntreBankSuspenseAccountNumber,
//                         CreditAccountName = tranlg.IntreBankSuspenseAccountName,
//                         CreditAmount = Convert.ToDecimal(interBankTotalDebitAmount),
//                         Narration = tranlg.Narration,
//                         CreditStatus = 2,
//                         BatchId = tranlg.BatchId,
//                         NameEnquiryStatus = 0,
//                         TryCount = 0,
//                         CorporateCustomerId = CorporateCustomerId,
//                         CreditDate = DateTime.Now,
//                     },
//                     new TblNipbulkCreditLog {
//                         Id = Guid.NewGuid(),
//                         TranLogId = tranlg.Id,
//                         CreditAccountNumber = tranlg.IntreBankSuspenseAccountNumber,
//                         CreditAccountName = tranlg.IntreBankSuspenseAccountName,
//                         CreditAmount = Convert.ToDecimal(totalVat),
//                         Narration = $"VCHG|{tranlg.Narration}"
//                     },
//                     new TblNipbulkCreditLog{
//                         Id = Guid.NewGuid(),
//                         TranLogId = tranlg.Id,
//                         CreditAccountNumber = tranlg.IntreBankSuspenseAccountNumber,
//                         CreditAccountName = tranlg.IntreBankSuspenseAccountName,
//                         CreditAmount = Convert.ToDecimal(totalFee),
//                         Narration = $"BCHG|{tranlg.Narration}"
//                     }
//                 });
//                 tranlg.InterBankStatus = 0;
//                 tranlg.TotalFee = totalFee;
//                 tranlg.TotalVat = totalVat;
//                 tranlg.InterBankTotalAmount = interBankTotalDebitAmount;
//             }    
//             if (intraBankCreditItems.Any())
//             {
//                 bulkSuspenseCreditItems.Add(new TblNipbulkCreditLog{
//                     Id = Guid.NewGuid(),
//                     TranLogId = tranlg.Id,
//                     CreditAccountNumber = tranlg.SuspenseAccountNumber,
//                     CreditAccountName = tranlg.SuspenseAccountName,
//                     CreditAmount = Convert.ToDecimal(intraBankTotalDebitAmount),
//                     Narration = tranlg.Narration,
//                     CreditBankCode = parallexBankCode,
//                 });
//                 tranlg.IntraBankTotalAmount = intraBankTotalDebitAmount;
//                 tranlg.IntraBankStatus = 0;
//             }
//             return bulkSuspenseCreditItems;
//         }

//         public List<TblCorporateApprovalHistory> SetAuthorizationWorkFlow(List<TblWorkflowHierarchy> workflowHierarchies, TblNipbulkTransferLog tranlg)
//         {
//             var tblCorporateApprovalHistories = new List<TblCorporateApprovalHistory>();
//             foreach (var item in workflowHierarchies)
//             {
//                 var toApproved = item.AuthorizationLevel == 1 ? 1 : 0;
//                 var corporateApprovalHistory = new TblCorporateApprovalHistory
//                 {
//                     Id = Guid.NewGuid(),
//                     LogId = tranlg.Id,
//                     Status = nameof(AuthorizationStatus.Pending),
//                     ApprovalLevel = item.AuthorizationLevel,
//                     ApproverName = item.ApproverName,
//                     Description = $"Authorizer {item.AuthorizationLevel}",
//                     Comment = "",
//                     UserId = item.ApproverId,
//                     ToApproved = toApproved,
//                     CorporateCustomerId = tranlg.CompanyId
//                 };
//                 _unitOfWork.CorporateApprovalHistoryRepo.Add(corporateApprovalHistory);
//             }
//             return tblCorporateApprovalHistories;
//         }

//         private TblNipbulkTransferLog PrepareBulkTransaction(TblCorporateSalarySchedule schedule,TblCorporateCustomer company,IConfiguration _config)
//         {
//             var batchId = Guid.NewGuid();
//             var tranlg = new TblNipbulkTransferLog
//             {
//                 Id = Guid.NewGuid(),
//                 Sn = 0,
//                 CompanyId = company.Id,
//                 InitiatorId = CorporateProfile.Id,
//                 DebitAccountName = schedule.AccountNumber,
//                 DebitAccountNumber = schedule.AccountName,
//                 Narration = $"BP|{batchId}|{schedule.Discription}|{company.CompanyName}",
//                 DateInitiated = DateTime.Now,
//                 PostingType = "Bulk",
//                 Currency = schedule.Currency,
//                 TransactionStatus = 0,
//                 TryCount = 0,
//                 TransferType = nameof(TransactionType.Salary),
//                 BatchId = batchId,
//                 ApprovalStatus = 0,
//                 ApprovalStage = 1,
//                 InitiatorUserName = CorporateProfile.Username,
//                 TransactionLocation = schedule.TransactionLocation,
//                 SuspenseAccountName = _config.GetValue<string>("NIPSBulkSuspenseAccountName"),
//                 SuspenseAccountNumber = _config.GetValue<string>("NIPSBulkSuspenseAccount"),
//                 IntreBankSuspenseAccountName= _config.GetValue<string>("NIPInterSBulkSuspenseAccountName"),
//                 IntreBankSuspenseAccountNumber= _config.GetValue<string>("NIPSInterBulkSuspenseAccount"),
//                 TotalCredits = 0,
//                 NoOfCredits = 0,
//                 InterBankTryCount = 0,
//                 InterBankTotalCredits = 0,
//                 Status = 0
//             };
//             return tranlg;
            
//         }

//         private async Task<TblNipbulkCreditLog> PrepareCreditbeneficiary(TblNipbulkTransferLog tranLog,TblCorporateCustomerEmployee employee,TblCorporateCustomer company,TblCorporateSalarySchedule schedule,BankListResponseData bankList, IReadOnlyList<TblFeeCharge> feeCharges, string parallexBankCode)
//         {
//             var items = await this.ValidateAccountNumber(employee.AccountNumber, employee.BankCode,bankList);
//             items.TranLogId = tranLog.Id;
//             items.CreditAmount = Convert.ToDecimal(employee.SalaryAmount);
//             items.Narration = $"BP|{tranLog.BatchId}|{schedule.Discription}|{company.CompanyName}";
//             items.BatchId = tranLog.BatchId;
//             items.CorporateCustomerId = tranLog.CompanyId;
//             items.InitiateDate = DateTime.Now;
//             if (items.CreditBankCode != parallexBankCode)
//             {
//                 var nipsCharge = NipsCharge.Calculate(feeCharges,(decimal)employee.SalaryAmount);
//                 items.Fee = nipsCharge.Fee;
//                 items.Vat = nipsCharge.Vat;
//             }
//             return items;
//         }

//         private async Task<TblNipbulkCreditLog> ValidateAccountNumber(string accountNumber,string accountCode ,BankListResponseData bankList)
//         {
//             var accountInfo = await _apiService.BankNameInquire(accountNumber, accountCode);
//             var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == accountCode);
//             var nipCreditInfo = new TblNipbulkCreditLog
//             {
//                 Id = Guid.NewGuid()
//             };
//             if (accountInfo.ResponseCode != "00")
//             {
//                 nipCreditInfo.CreditAccountNumber = accountInfo.AccountNumber;
//                 nipCreditInfo.CreditAccountName = accountInfo.AccountName;
//                 nipCreditInfo.CreditBankCode =accountCode;
//                 nipCreditInfo.CreditBankName = bank.InstitutionName;
//                 nipCreditInfo.CreditStatus = 2;
//                 nipCreditInfo.NameEnquiryRef = accountInfo.RequestId;
//                 nipCreditInfo.ResponseCode = accountInfo.ResponseCode;
//                 nipCreditInfo.ResponseMessage = accountInfo.ResponseMessage;
//                 nipCreditInfo.NameEnquiryStatus = 0;
//                 nipCreditInfo. TryCount = 0;
//             }
//             else
//             {
//                 nipCreditInfo.CreditAccountNumber = accountInfo.AccountNumber;
//                 nipCreditInfo.CreditAccountName = accountInfo.AccountName;
//                 nipCreditInfo.CreditBankCode =accountCode;
//                 nipCreditInfo.CreditBankName = bank.InstitutionName;
//                 nipCreditInfo.CreditStatus = 0;
//                 nipCreditInfo.NameEnquiryRef = accountInfo.RequestId;
//                 nipCreditInfo.ResponseCode = accountInfo.ResponseCode;
//                 nipCreditInfo.ResponseMessage = accountInfo.ResponseMessage;
//                 nipCreditInfo.NameEnquiryStatus = 1;
//                 nipCreditInfo. TryCount = 0;
//             }
//             return nipCreditInfo;
//         }

//         private TblCorporateSalarySchedule MapCreateRequestDtoToCorporateCustomerSalary(CreateCorporateCustomerSalaryDto payload)
//         {
//             var mapEmployee = Mapper.Map<TblCorporateSalarySchedule>(payload);
//             mapEmployee.Status =(int) ProfileStatus.Pending;
//             mapEmployee.InitiatorId = CorporateProfile.Id;
//             mapEmployee.DateCreated = DateTime.Now;
//             mapEmployee.Sn = 0;
//             mapEmployee.Id = Guid.NewGuid();
//             return mapEmployee;
//         }

//         private TblTempCorporateSalarySchedule MapCreateRequestDtoToTempCorporateCustomerSalary(CreateCorporateCustomerSalaryDto payload)
//         {
//             var mapEmployee = Mapper.Map<TblTempCorporateSalarySchedule>(payload);
//             mapEmployee.Status =(int) ProfileStatus.Pending;
//             mapEmployee.InitiatorId = CorporateProfile.Id;
//             mapEmployee.DateCreated = DateTime.Now;
//             mapEmployee.Sn = 0;
//             mapEmployee.Id = Guid.NewGuid();
//             return mapEmployee;
//         }
        
//         private TblCorporateSalarySchedule MapUpdateRequestDtoToCorporateCustomerSalary(UpdateCorporateCustomerSalaryDto payload)
//         {
//             var mapEmployee = Mapper.Map<TblCorporateSalarySchedule>(payload);
//             mapEmployee.Status =(int) ProfileStatus.Pending;
//             mapEmployee.InitiatorId = CorporateProfile.Id;
//             mapEmployee.DateCreated = DateTime.Now;
//             mapEmployee.Sn = 0;
//             mapEmployee.Id = Guid.NewGuid();
//             return mapEmployee;
//         }
//         private TblTempCorporateSalarySchedule MapUpdateRequestDtoToTempCorporateCustomerSalary(UpdateCorporateCustomerSalaryDto payload)
//         {
//             var mapEmployee = Mapper.Map<TblTempCorporateSalarySchedule>(payload);
//             mapEmployee.Status =(int) ProfileStatus.Pending;
//             mapEmployee.InitiatorId = CorporateProfile.Id;
//             mapEmployee.DateCreated = DateTime.Now;
//             mapEmployee.Sn = 0;
//             mapEmployee.Id = Guid.NewGuid();
//             return mapEmployee;
//         } 
//         private List<TblNipbulkCreditLog> PrepareBulkTransactionCharges(TblNipbulkTransferLog creditLog, string parallexBankCode, string parralexBank)
//         {
//             var nipBulkCreditLogRepo = new List<TblNipbulkCreditLog>();
//             nipBulkCreditLogRepo.AddRange(new [] {
//                 new TblNipbulkCreditLog{
//                   Id = Guid.NewGuid(),
//                   TranLogId = creditLog.Id,
//                   CreditAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
//                   CreditAccountName = creditLog.IntreBankSuspenseAccountName,
//                   CreditAmount = Convert.ToDecimal(creditLog.TotalVat),
//                   CreditBankCode = parallexBankCode,
//                   CreditBankName = parralexBank,
//                   Narration = $"VCHG|{creditLog.Narration}",
//                   CreditStatus = 2,
//                   BatchId = creditLog.BatchId,
//                   NameEnquiryStatus = 0,
//                   TryCount = 0,
//                   CorporateCustomerId = creditLog.CompanyId,
//                   CreditDate = DateTime.Now,
//               },
//               new TblNipbulkCreditLog{
//                 Id = Guid.NewGuid(),
//                 TranLogId = creditLog.Id,
//                 CreditAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
//                 CreditAccountName = creditLog.IntreBankSuspenseAccountName,
//                 CreditAmount = Convert.ToDecimal(creditLog.TotalFee),
//                 CreditBankCode = parallexBankCode,
//                 CreditBankName = parralexBank,
//                 Narration = $"BCHG|{creditLog.Narration}",
//                 CreditStatus = 2,
//                 BatchId = creditLog.BatchId,
//                 NameEnquiryStatus = 0,
//                 TryCount = 0,
//                 CorporateCustomerId = creditLog.CompanyId,
//                 CreditDate = DateTime.Now,
//             }});
//             return nipBulkCreditLogRepo;
//         }
//         private ValidationStatus ValidateWorkflowAccess(Guid? workflowId, decimal amount)
//         {
//             if (workflowId != null)
//             {
//                 var workFlow = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)workflowId);
//                 if (workFlow == null)
//                 {
//                     return new ValidationStatus { Status = false, Message = "Workflow is invalid" };
//                 }

//                 if (workFlow.Status != 1)
//                 {
//                     return new ValidationStatus { Status = false, Message = "Workflow selected is not active" };
//                 }

//                 var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(workFlow.Id);
//                 if (workflowHierarchies.Count == 0)
//                 {
//                     return new ValidationStatus { Status = false, Message = "No Workflow Hierarchies found" };
//                 }
//                 if (workflowHierarchies.Count != workFlow.NoOfAuthorizers)
//                 {
//                     return new ValidationStatus { Status = false, Message = "Workflow Authorize is not valid " };
//                 }

//             }
//             return new ValidationStatus { Status = true, Message = "Validation OK" };
//         }
//         private bool DailyLimitExceeded(TblCorporateCustomer tblCorporateCustomer, decimal amount, out string errorMsg)
//         {
//             errorMsg = string.Empty;
//             var customerDailyTransLimitHistory = _unitOfWork.TransactionHistoryRepo.GetTransactionHistory(tblCorporateCustomer.Id, DateTime.Now.Date);
//             if (customerDailyTransLimitHistory != null)
//             {
//                 if (tblCorporateCustomer.BulkTransDailyLimit != null)
//                 {
//                     if (customerDailyTransLimitHistory.BulkTransTotalAmount != null)
//                     {
//                     decimal amtTransferable = (decimal)tblCorporateCustomer.BulkTransDailyLimit - (decimal)customerDailyTransLimitHistory.BulkTransTotalAmount;

//                     if (amtTransferable < amount)
//                     {
//                         if(amtTransferable <= 0)
//                         {
//                             errorMsg = $"You have exceeded your daily bulk transaction limit Which is {Helper.formatCurrency(tblCorporateCustomer.BulkTransDailyLimit)}";
//                             return true;
//                         }
//                         errorMsg = $"Transaction amount {Helper.formatCurrency(amount)} has exceeded the maximum daily transaction limit {Helper.formatCurrency(tblCorporateCustomer.BulkTransDailyLimit)} for your organisation. You can only transfer {Helper.formatCurrency(amtTransferable)} for the rest of the day";
//                         return true;
//                     }
//                     }
//                 }
//             }
//             return false;
//         }
//         private void AddAuditTrial(AuditTrailDetail info)
//         {
//             var auditTrail = new TblAuditTrail
//             {
//                 Id = Guid.NewGuid(),
//                 ActionCarriedOut = info.Action,
//                 Ipaddress = info.Ipaddress,
//                 Macaddress = info.Macaddress,
//                 HostName = info.HostName,
//                 ClientStaffIpaddress = info.ClientStaffIpaddress,
//                 NewFieldValue = info.NewFieldValue,
//                 PreviousFieldValue = info.PreviousFieldValue,
//                 TransactionId = "",
//                 UserId = info.UserId,
//                 Username = info.UserName,
//                 Description = $"{info.Description}",
//                 TimeStamp = DateTime.Now
//             };
//             _unitOfWork.AuditTrialRepo.Add(auditTrail);
//         }
//         private void SendForAuthorization(List<TblCorporateApprovalHistory> workflowHierarchies, TblNipbulkTransferLog tranlg)
//         {
//             var firstApproval = workflowHierarchies.First(ctx => ctx.ApprovalLevel == 1);
//             var corporateUser = _unitOfWork.CorporateProfileRepo.GetByIdAsync(firstApproval.UserId.Value);
//             var initiatorName = _unitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)tranlg.InitiatorId);
//             ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(corporateUser.Email, initiatorName.FullName,string.Format("{0:0.00}", tranlg.DebitAmount) )));
//         }
//         private static BulkIntrabankTransactionModel FormatBulkTransaction(List<TblNipbulkCreditLog> bulkTransaction,  TblNipbulkTransferLog creditLog)
//         {
//             var narrationTuple = creditLog.Narration.Length > 50 ? Tuple.Create(creditLog.Narration[..50],creditLog.Narration[50..]) :  Tuple.Create(creditLog.Narration,"");
//             var tranDate =  DateTime.Now;
//             var creditItems = new List<PartTrnRec>();
//             var beneficiary = new PartTrnRec{
//                 AcctId = creditLog.DebitAccountNumber,
//                 CreditDebitFlg = "D",
//                 TrnAmt = creditLog.DebitAmount.ToString(),
//                 currencyCode = "NGN",
//                 TrnParticulars = narrationTuple.Item1,
//                 ValueDt = tranDate.ToString("MM/dd/yyyy HH:mm:ss"),
//                 PartTrnRmks = Generate16DigitNumber.Create16DigitString(),
//                 REFNUM= "",
//                 RPTCODE = "",
//                 TRANPARTICULARS2= narrationTuple.Item2
//             };
//             creditItems.Add(beneficiary);
//             foreach(var item in bulkTransaction)
//             {
//                 var tranNarration = item.Narration.Length > 50 ? Tuple.Create(item.Narration[..50],item.Narration[50..]) :  Tuple.Create(item.Narration,"");
//                 var creditBeneficiary = new PartTrnRec {
//                     AcctId =  item.CreditAccountNumber,
//                     CreditDebitFlg = "C",
//                     TrnAmt = item.CreditAmount.ToString(),
//                     currencyCode = "NGN",
//                     TrnParticulars = tranNarration.Item1,
//                     ValueDt = tranDate.ToString("MM/dd/yyyy HH:mm:ss"),
//                     PartTrnRmks =  Generate16DigitNumber.Create16DigitString(),
//                     REFNUM = "",
//                     RPTCODE = "",
//                     TRANPARTICULARS2 = tranNarration.Item2
//                 };
//                 creditItems.Add(creditBeneficiary);
//             };
//             var intraBankBulkTransfer = new BulkIntrabankTransactionModel {
//             BankId = "01",
//             TrnType ="T",
//             TrnSubType ="CI",
//             RequestID = Generate16DigitNumber.Create16DigitString(),
//             PartTrnRec = creditItems,
//             };
//             return intraBankBulkTransfer;
//         }
//         private async Task<List<IntraBankTransferResponse>> ProcessBulkTransactionCharges(TblNipbulkTransferLog creditLog,  string parallexBankCode, string parralexBank)
//         {
//             var responseResult = new List<IntraBankTransferResponse>();
//             var bulkTransaction = this.PrepareBulkTransactionCharges(creditLog,parallexBankCode,parralexBank);
//             foreach(var item in bulkTransaction)
//             {
//             var narrationTuple = item.Narration.Length > 50 ? Tuple.Create(item.Narration[..50],item.Narration[50..]) :  Tuple.Create(item.Narration,"");
//             var date = DateTime.Now;
//             var transfer = new IntraBankPostDto {
//                 AccountToDebit = creditLog.DebitAccountNumber,
//                 UserName = CorporateProfile.Username,
//                 Channel = "2",
//                 TransactionLocation = creditLog.TransactionLocation,
//                 IntraTransferDetails = new List<IntraTransferDetail>{
//                     new IntraTransferDetail {
//                         TransactionReference = Generate16DigitNumber.Create16DigitString(),
//                         TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
//                         BeneficiaryAccountName = creditLog.IntreBankSuspenseAccountName,
//                         BeneficiaryAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
//                         Amount = item.CreditAmount,
//                         Narration = narrationTuple.Item1
//                     }
//                 }
//             };
//             var transferResult = await _apiService.IntraBankTransfer(transfer);
//             if(transferResult.ResponseCode != "00")
//             {
//                 //transferResult.HasFailed = true;
//                 responseResult.Add(transferResult);
//             }
//             else
//             {
//                 //transferResult.HasFailed = false;
//                 responseResult.Add(transferResult);
//             }
//             }
//             return responseResult;
//         }
//         public async Task<List<TblNipbulkCreditLog>> ScheduleBeneficiaries (List<TblCorporateSalaryScheduleBeneficiary> beneficiaries,TblCorporateSalarySchedule schedule, TblCorporateCustomer company,BankListResponseData bankList, IConfiguration _config,IReadOnlyList<TblFeeCharge> feeCharges)
//         {
//             var beneficairiesList = new List<TblNipbulkCreditLog>();
//             var tranlg = PrepareBulkTransaction(schedule,company,_config);
//             var parallexBankCode =  _config.GetValue<string>("ParralexBankCode");
//            foreach( var beneficiary in beneficiaries)
//            {
//                 var items = await this.PrepareScheduleBeneficiary(beneficiary,parallexBankCode,tranlg,bankList,feeCharges);
//                 if(items != null)
//                 {
//                     items.Narration = $"BP|{tranlg.BatchId}|{schedule.Discription}|{company.CompanyName}";
//                     beneficairiesList.Add(items);
//                 }
//            }
//            return beneficairiesList;
//         }
//         public async Task<TblNipbulkCreditLog> PrepareScheduleBeneficiary (TblCorporateSalaryScheduleBeneficiary item,string parallexBankCode,TblNipbulkTransferLog tranlg,BankListResponseData bankList,IReadOnlyList<TblFeeCharge> feeCharges)
//         {
//             var employee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync((Guid)item.EmployeeId);
//             if(employee != null)
//             {
//                 return null;
//             }
//             var items = await this.ValidateAccountNumber(employee.AccountNumber, employee.BankCode,bankList);
//             items.TranLogId = tranlg.Id;
//             items.CreditAmount = Convert.ToDecimal(item.Amount);
//             items.BatchId = tranlg.BatchId;
//             items.CorporateCustomerId = CorporateProfile.CorporateCustomerId;
//             items.InitiateDate = DateTime.Now;
//             if (items.CreditBankCode != parallexBankCode)
//             {
//                 var nipsCharge = NipsCharge.Calculate(feeCharges,(decimal)item.Amount);
//                 items.Fee = nipsCharge.Fee;
//                 items.Vat = nipsCharge.Vat;
//             }
//             return items;
//         }
//         public void ProcessFailedBulkTransaction(BulkIntraBankTransactionResponse postBulkIntraBankBulk,TblNipbulkTransferLog tranlg, string parralexBank)
//         {
//             _logger.LogError("TRANSACTION ERROR {0}, {1}, {2}",Formater.JsonType(postBulkIntraBankBulk.ResponseCode), Formater.JsonType(postBulkIntraBankBulk.ResponseMessage), Formater.JsonType(postBulkIntraBankBulk.ErrorDetail));
           
//             if(tranlg.InterBankTotalAmount > 0)
//             {
//                 UnitOfWork.TransactionRepo.Add(new TblTransaction {
//                     Id = Guid.NewGuid(),
//                     TranAmout = tranlg.InterBankTotalAmount,
//                     DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
//                     DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
//                     DesctionationBank = parralexBank,
//                     TranType = "bulk",
//                     TransactionStatus = nameof(TransactionStatus.Failed),
//                     Narration = $"{tranlg.Narration}|inter",
//                     SourceAccountName = tranlg.DebitAccountName,
//                     SourceAccountNo = tranlg.DebitAccountNumber,
//                     SourceBank = parralexBank,
//                     CustAuthId = CorporateProfile.Id,
//                     Channel = "WEB",
//                     TransactionReference = postBulkIntraBankBulk.TrnId,
//                     ResponseCode = postBulkIntraBankBulk.ResponseCode,
//                     ResponseDescription= postBulkIntraBankBulk.ResponseMessage,
//                     TranDate = DateTime.Now,
//                     CorporateCustomerId = CorporateProfile.CorporateCustomerId,
//                     BatchId = tranlg.BatchId 
//                 });
//             }
//             if(tranlg.IntraBankTotalAmount > 0)
//             {
//                 UnitOfWork.TransactionRepo.Add(new TblTransaction {
//                     Id = Guid.NewGuid(),
//                     TranAmout = tranlg.IntraBankTotalAmount,
//                     DestinationAcctName = tranlg.SuspenseAccountName,
//                     DestinationAcctNo = tranlg.SuspenseAccountNumber,
//                     DesctionationBank = parralexBank,
//                     TranType = "bulk",
//                     TransactionStatus = nameof(TransactionStatus.Failed),
//                     Narration = $"{tranlg.Narration}|intra",
//                     SourceAccountName = tranlg.DebitAccountName,
//                     SourceAccountNo = tranlg.DebitAccountNumber,
//                     SourceBank = parralexBank,
//                     CustAuthId = CorporateProfile.Id,
//                     Channel = "WEB",
//                     TransactionReference = postBulkIntraBankBulk.TrnId,
//                     ResponseCode = postBulkIntraBankBulk.ResponseCode,
//                     ResponseDescription= postBulkIntraBankBulk.ResponseMessage,
//                     TranDate = DateTime.Now,
//                     CorporateCustomerId = CorporateProfile.CorporateCustomerId,
//                     BatchId = tranlg.BatchId  
//                 });
//             }
//             tranlg.ResponseCode = postBulkIntraBankBulk.ResponseCode;
//             tranlg.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
//             tranlg.ErrorDetail= Formater.JsonType(postBulkIntraBankBulk.ErrorDetail);  
//             tranlg.Status = 2;
//             tranlg.TransactionStatus = 2;
//             tranlg.ApprovalStatus = 1;
//             tranlg.TransactionReference = postBulkIntraBankBulk.TrnId;
//             UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
//         }
//         private void ProcessSuccessfulBulkTransaction(BulkIntraBankTransactionResponse postBulkIntraBankBulk,TblNipbulkTransferLog tranlg, string parralexBank, DateTime tranDate)
//         {
           
//             if(tranlg.InterBankTotalAmount > 0 )
//             {
//                 UnitOfWork.TransactionRepo.AddRange(new [] {
//                     new TblTransaction {
//                         Id = Guid.NewGuid(),
//                         TranAmout = tranlg.InterBankTotalAmount,
//                         DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
//                         DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
//                         DesctionationBank = parralexBank,
//                         TranType = "bulk",
//                         TransactionStatus = nameof(TransactionStatus.Successful),
//                         Narration = $"{tranlg.Narration}|inter",
//                         SourceAccountName = tranlg.DebitAccountName,
//                         SourceAccountNo = tranlg.DebitAccountNumber,
//                         SourceBank = parralexBank,
//                         CustAuthId = CorporateProfile.Id,
//                         Channel = "WEB",
//                         TransactionReference = postBulkIntraBankBulk.TrnId,
//                         ResponseCode = postBulkIntraBankBulk.ResponseCode,
//                         ResponseDescription= postBulkIntraBankBulk.ResponseMessage,
//                         TranDate = DateTime.Now,
//                         CorporateCustomerId = CorporateProfile.CorporateCustomerId,
//                         BatchId = tranlg.BatchId
//                     },
//                 });
//             }
//             if(tranlg.IntraBankTotalAmount > 0)
//             {
//                 UnitOfWork.TransactionRepo.Add(new TblTransaction {
//                     Id = Guid.NewGuid(),
//                     TranAmout = tranlg.IntraBankTotalAmount,
//                     DestinationAcctName = tranlg.SuspenseAccountName,
//                     DestinationAcctNo = tranlg.SuspenseAccountNumber,
//                     DesctionationBank = parralexBank,
//                     TranType = "bulk",
//                     TransactionStatus = nameof(TransactionStatus.Successful),
//                     Narration = $"{tranlg.Narration}|intra",
//                     SourceAccountName = tranlg.DebitAccountName,
//                     SourceAccountNo = tranlg.DebitAccountNumber,
//                     SourceBank = parralexBank,
//                     CustAuthId = CorporateProfile.Id,
//                     Channel = "WEB",
//                     TransactionReference = postBulkIntraBankBulk.TrnId,
//                     ResponseCode = postBulkIntraBankBulk.ResponseCode,
//                     ResponseDescription= postBulkIntraBankBulk.ResponseMessage,
//                     TranDate = DateTime.Now,
//                     CorporateCustomerId = CorporateProfile.CorporateCustomerId,
//                     BatchId = tranlg.BatchId 
//                 });
//             }
//             tranlg.ResponseCode = postBulkIntraBankBulk.ResponseCode;
//             tranlg.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
//             tranlg.Status = 1;
//             tranlg.DateProccessed = tranDate;
//             tranlg.ApprovalStatus = 1;
//             tranlg.ApprovalCount = 1;
//             tranlg.ApprovalStage = 1;
//             tranlg.TransactionStatus = 0;
//             tranlg.TransactionReference = postBulkIntraBankBulk.TrnId;
//             UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
//         }
//         private async Task<List<TblCorporateCustomerEmployee>>CorporeCustomerEmployees(TblCorporateSalarySchedule customer)
//         {
//             return await UnitOfWork.CorporateEmployeeRepo.GetCorporateCustomerEmployees((Guid)customer.CorporateCustomerId);
//         }
//         private async Task<List<TblCorporateSalaryScheduleBeneficiary>>CorporateBeneficairies(TblCorporateSalarySchedule entity)
//         {
//             return await UnitOfWork.ScheduleBeneficairyRepo.GetScheduleBeneficiaries(entity);
//         }
//         private async Task<List<TblNipbulkCreditLog>> PrepareEmployeePayroll(List<TblCorporateCustomerEmployee> employees, TblCorporateSalarySchedule schedule,TblCorporateCustomer company,BankListResponseData bankList,IConfiguration _config,IReadOnlyList<TblFeeCharge> feeCharges)
//         {
//             var transactionItem = new List<TblNipbulkCreditLog>();
//             var tranlg = PrepareBulkTransaction(schedule,company,_config);
//             var parallexBankCode =  _config.GetValue<string>("ParralexBankCode");
//             foreach(var employee in employees)
//             {
//                 var item = await this.PrepareCreditbeneficiary(tranlg,employee,company,schedule,bankList,feeCharges,parallexBankCode);
//                 transactionItem.Add(item);
//             }
//             return transactionItem;
//         }
//   }
// }