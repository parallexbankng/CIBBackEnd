
using CIB.IntraBankTransactionService.Entities;
using CIB.IntraBankTransactionService.Modules.Common.Interface;
using CIB.IntraBankTransactionService.Services;
using CIB.IntraBankTransactionService.Services.Request;
using CIB.IntraBankTransactionService.Utils;
using Newtonsoft.Json;
using TransactionStatus = CIB.IntraBankTransactionService.Utils.TransactionStatus;

namespace CIB.IntraBankTransactionService.Jobs;

public class IntraBankJob : IIntraBankJob
{

  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<IntraBankJob> _logger;

  public IntraBankJob( IServiceProvider serviceProvider,ILogger<IntraBankJob> logger)
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
      int maxTryCount = int.Parse(config.GetValue<string>("maxTryCount"));
      var pendingTransfers = unitOfWork.BulkPaymentLogRepo.GetPendingTransferItems(0,50,maxTryCount);
      if (pendingTransfers.Count != 0)
      {
        var processDuration = DateTime.Now.Date;
        foreach (var i in pendingTransfers)
        {
          var pendingCreditItemList = unitOfWork.BulkCreditLogRepo.GetPendingCredit(i.Id, i.TryCount > 0 ? 2 : 0, prallexBankCode, processDuration);
          if (pendingCreditItemList.Any())
          {
            foreach (var creditLog in pendingCreditItemList)
            {
              var date = DateTime.Now;
              decimal creditAmount = creditLog.CreditAmount ?? 0;
              var transaction = new TblTransaction
              {
                Id = Guid.NewGuid(),
                TranAmout = creditAmount,
                TranDate = DateTime.Now,
                SourceAccountNo = i.IntreBankSuspenseAccountNumber,
                SourceAccountName = i.IntreBankSuspenseAccountName,
                SourceBank = prallexBankCode,
                TranType = "Interbank transfer",
                Narration = i.Narration,
                Channel = "WEB",
                DesctionationBank = creditLog.CreditBankCode,
                DestinationAcctNo = creditLog.CreditAccountNumber,
                DestinationAcctName = creditLog.CreditBankName,
                CorporateCustomerId = i.CompanyId,
                BatchId = i.BatchId
              };

              var tranRef = creditLog.TryCount == 0 ? creditLog.TransactionReference : Transactions.Ref();
              if(creditLog.TryCount > 0)
              {
                string[]? sessionIds = creditLog?.SessionId.Split('|');
                var getLastSessionId =  sessionIds.Last();
                var query = new  RequeryTransaction()
                {
                  UserName = i.InitiatorUserName,
                  TransactionReference = getLastSessionId,
                  BeneficiaryAccountNumber = creditLog.CreditAccountNumber,
                  BeneficiaryBankCode = creditLog.CreditBankCode,
                  AccountToDebit = i.SuspenseAccountNumber,
                  Amount = creditLog.CreditAmount
                };
                var queryResult = await apiService.QueryTransferTransaction(query);
                if(queryResult.ResponseCode != "00")
                {
                  var newTranRef  = string.Concat(creditLog.TransactionReference + $"|{tranRef}");
                  var transfer = new PostIntraBankTransaction
                  {
                    AccountToDebit = i.SuspenseAccountNumber,
                    UserName = i.InitiatorUserName,
                    Channel = "2",
                    TransactionLocation = i.TransactionLocation,
                    IntraTransferDetails = new List<IntraTransferDetail>
                    {
                      new IntraTransferDetail
                      {
                        TransactionReference = tranRef,
                        TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
                        BeneficiaryAccountName = creditLog.CreditAccountName,
                        BeneficiaryAccountNumber = creditLog.CreditAccountNumber,
                        Amount = creditLog.CreditAmount,
                        Narration = creditLog.Narration,
                      }
                    },
                  };

                  var result = await apiService.PostIntraBankTransfer(transfer);
                  creditLog.TryCount = (creditLog.TryCount ?? 0) + 1;
                  creditLog.CreditDate = DateTime.Now;
                  creditLog.TransactionResponseMessage = result.ResponseDescription;
                  creditLog.TransactionResponseCode = result.ResponseCode;
                  creditLog.SessionId = string.Concat(creditLog.SessionId + $"|{result.TransactionReference}");
                  transaction.TransactionReference = tranRef;
                  transaction.SessionId = result.TransactionReference;

                  if (result.ResponseCode != "00")
                  {
                    _logger.LogError("TRANSACTION ERROR {0}, {1}",JsonConvert.SerializeObject(result.ResponseCode), JsonConvert.SerializeObject(result.ResponseDescription));
                  
                    transaction.TransactionStatus = nameof(TransactionStatus.Failed);
                    creditLog.CreditStatus = 2;
                    creditLog.TransactionReference = newTranRef;
                    unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                    unitOfWork.TransactionRepo.Add(transaction);
                    unitOfWork.Complete();
                  }
                  else
                  {
                    transaction.TransactionStatus = nameof(TransactionStatus.Successful);
                    creditLog.CreditStatus = 1;
                    unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                    unitOfWork.TransactionRepo.Add(transaction);
                    unitOfWork.Complete();
                  }
                }
                else
                {
                  string[]? transactionRef = creditLog?.TransactionReference.Split('|');
                  var lastTransactionRef =  transactionRef.Last();
                  transaction.TransactionReference = lastTransactionRef;
                  transaction.SessionId = queryResult.TransactionReference;
                  transaction.TransactionStatus = nameof(TransactionStatus.Successful);
                  creditLog.CreditStatus = 1;
                  unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                  unitOfWork.TransactionRepo.Add(transaction);
                  unitOfWork.Complete();
                }
              }
              else
              {

                var transfer = new PostIntraBankTransaction
                {
                  AccountToDebit = i.SuspenseAccountNumber,
                  UserName = i.InitiatorUserName,
                  Channel = "2",
                  TransactionLocation = i.TransactionLocation,
                  IntraTransferDetails = new List<IntraTransferDetail>
                  {
                    new IntraTransferDetail
                    {
                      TransactionReference = tranRef,
                      TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
                      BeneficiaryAccountName = creditLog.CreditAccountName,
                      BeneficiaryAccountNumber = creditLog.CreditAccountNumber,
                      Amount = creditLog.CreditAmount,
                      Narration = creditLog.Narration,
                    }
                  },
                };

                var result = await apiService.PostIntraBankTransfer(transfer);
                creditLog.TryCount = (creditLog.TryCount ?? 0) + 1;
                creditLog.CreditDate = DateTime.Now;
                creditLog.TransactionResponseMessage = result.ResponseDescription;
                creditLog.TransactionResponseCode = result.ResponseCode;
                creditLog.SessionId = result.TransactionReference;
                transaction.TransactionReference = tranRef;
                transaction.SessionId = result.TransactionReference;

                if (result.ResponseCode != "00")
                {
                  _logger.LogError("TRANSACTION ERROR {0}, {1}",JsonConvert.SerializeObject(result.ResponseCode), JsonConvert.SerializeObject(result.ResponseDescription));
                
                  transaction.TransactionStatus = nameof(TransactionStatus.Failed);
                  creditLog.CreditStatus = 2;
                  unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                  unitOfWork.TransactionRepo.Add(transaction);
                  unitOfWork.Complete();
                }
                else
                {
                  transaction.TransactionStatus = nameof(TransactionStatus.Successful);
                  creditLog.CreditStatus = 1;
                  unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                  unitOfWork.TransactionRepo.Add(transaction);
                  unitOfWork.Complete();
                }
              }
            }
          }
          if (i.TryCount < maxTryCount)
          {
            var totalCredit = unitOfWork.BulkPaymentLogRepo.GetInterBankTotalCredit(i.Id, prallexBankCode, processDuration);
            i.TotalCredits = totalCredit;
            if (pendingCreditItemList.Count == 0)
            {
              var checkFailedTransaction = unitOfWork.BulkCreditLogRepo.CheckForPendingCredit(i.Id, 2, processDuration);
              if (checkFailedTransaction.Count == 0 && i.InterBankStatus != 0)
              {
                i.TransactionStatus = 1;
                i.IntraBankStatus = 1;
              }
              else
              {
                i.IntraBankStatus = 1;
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
