using CIB.InterBankTransactionService.Entities;
using CIB.InterBankTransactionService.Modules.Common.Interface;
using CIB.InterBankTransactionService.Services;
using CIB.InterBankTransactionService.Services.Request;
using CIB.InterBankTransactionService.Utils;
using Newtonsoft.Json;
using Serilog;
using TransactionStatus = CIB.InterBankTransactionService.Utils.TransactionStatus;

namespace CIB.InterBankTransactionService.Jobs;

public class InterBankJob : IInterBankJob
{

  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<InterBankJob> _logger;

  public InterBankJob( IServiceProvider serviceProvider,ILogger<InterBankJob> logger)
  {
    this._serviceProvider = serviceProvider;
    this._logger = logger;
  }
  public async Task Run()
  {
    try
    {
      
      using var scope = _serviceProvider.CreateScope();
      var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
      var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
      var apiService = scope.ServiceProvider.GetRequiredService<IApiService>();
      string prallexBankCode = config.GetValue<string>("parralexBankCode");
      int maxTryCount = int.Parse(config.GetValue<string>("retryCount"));
      var pendingTransfers = unitOfWork.BulkPaymentLogRepo.GetPendingTransferItems(0,50,maxTryCount);
     
      if (pendingTransfers.Count != 0)
      {
          var processDuration = DateTime.Now.Date;
          foreach (TblNipbulkTransferLog i in pendingTransfers)
          {
            var pendingCreditItemList = unitOfWork.BulkCreditLogRepo.GetPendingCredit(i.Id, i.TryCount > 0 ? 2 : 0, prallexBankCode, processDuration);
            if (pendingCreditItemList.Count != 0)
            {
              foreach (var creditLog in pendingCreditItemList)
              {
                decimal creditAmount = creditLog.CreditAmount ?? 0;
                var date = DateTime.Now;
                var transfer = new PostInterBankTransaction{
                  accountToDebit = i.IntreBankSuspenseAccountNumber,
                  userName = i.InitiatorUserName,
                  channel = "2",
                  transactionLocation = i.TransactionLocation,
                  interTransferDetails = new List<InterTransferDetail>{
                    new InterTransferDetail {
                      transactionReference = Transactions.Ref(),
                      beneficiaryAccountName = creditLog.CreditAccountName,
                      beneficiaryAccountNumber = creditLog.CreditAccountNumber,
                      transactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
                      amount = creditLog.CreditAmount,
                      customerRemark = creditLog.Narration,
                      beneficiaryBVN = creditLog.BankVerificationNo,
                      beneficiaryKYC = creditLog.KycLevel,
                      beneficiaryBankCode = creditLog.CreditBankCode,
                      beneficiaryBankName = creditLog.CreditBankName,
                      nameEnquirySessionID = creditLog.NameEnquiryRef
                    }
                  },
                };
                var result = await apiService.PostInterBankTransfer(transfer);
                if (result.ResponseCode != "00")
                {
                  var transaction = new TblTransaction{
                    Id = Guid.NewGuid(),
                    TransactionReference = result.TransactionReference,
                    TranAmout = creditAmount,
                    TranDate = DateTime.Now,
                    SourceAccountNo = i.SuspenseAccountNumber,
                    SourceAccountName = i.SuspenseAccountName,
                    SourceBank = prallexBankCode,
                    TransactionStatus = nameof(TransactionStatus.Failed),
                    TranType = "Interbank transfer",
                    Narration = i.Narration,
                    Channel = "WEB",
                    DesctionationBank = creditLog.CreditBankCode,
                    DestinationAcctNo = creditLog.CreditAccountNumber,
                    DestinationAcctName = creditLog.CreditBankName,
                    CorporateCustomerId = i.CompanyId,
                    BatchId = i.BatchId
                  };
                  creditLog.TryCount = (creditLog.TryCount ?? 0) + 1;
                  creditLog.CreditStatus = 2;
                  creditLog.CreditDate = DateTime.Now;
                  creditLog.ResponseMessage = result.ResponseDescription;
                  creditLog.ResponseCode = result.ResponseCode;
                  creditLog.TransactionReference = result.TransactionReference;
                  unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                  unitOfWork.TransactionRepo.Add(transaction);
                  unitOfWork.Complete();
                 }
                else
                {
                  var transaction = new TblTransaction{
                    Id = Guid.NewGuid(),
                    TransactionReference = result.TransactionReference,
                    TranAmout = creditAmount,
                    TranDate = DateTime.Now,
                    SourceAccountNo = i.SuspenseAccountNumber,
                    SourceAccountName = i.SuspenseAccountName,
                    SourceBank = prallexBankCode,
                    TransactionStatus = nameof(TransactionStatus.Successful),
                    TranType = "Interbank transfer",
                    Narration = i.Narration,
                    Channel = "WEB",
                    DesctionationBank = creditLog.CreditBankCode,
                    DestinationAcctNo = creditLog.CreditAccountNumber,
                    DestinationAcctName = creditLog.CreditBankName,
                    CorporateCustomerId = i.CompanyId,
                    BatchId = i.BatchId
                  };
                  creditLog.TryCount = (creditLog.TryCount ?? 0) + 1;
                  creditLog.CreditStatus = 1;
                  creditLog.CreditDate = DateTime.Now;
                  creditLog.ResponseMessage = result.ResponseDescription;
                  creditLog.ResponseCode = result.ResponseCode;
                  creditLog.TransactionReference = result.TransactionReference;
                  unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                  unitOfWork.TransactionRepo.Add(transaction);
                  unitOfWork.Complete();
                }
              }
            }

            if (i.TryCount < maxTryCount)
            {
              var totalCredit = unitOfWork.BulkCreditLogRepo.GetInterBankTotalCredit(i.Id, prallexBankCode, processDuration);
              i.TotalCredits = totalCredit;
              if (pendingCreditItemList.Count == 0)
              {
                var checkFailedTransaction = unitOfWork.BulkCreditLogRepo.CheckForPendingCredit(i.Id, 2, processDuration);
                if (checkFailedTransaction.Count == 0 && i.IntraBankStatus !=0)
                {
                  i.TransactionStatus = 1;
                  i.InterBankStatus = 1;
                }
                else
                {
                  i.InterBankStatus = 1;
                }
              }
              i.TryCount++;
              unitOfWork.BulkPaymentLogRepo.UpdateStatus(i);
              unitOfWork.Complete();
            }
            
          }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError("SERVER ERROR {0}, {1}, {2}",JsonConvert.SerializeObject(ex.StackTrace), JsonConvert.SerializeObject(ex.Source), JsonConvert.SerializeObject(ex.Message));
    }
  }
}
