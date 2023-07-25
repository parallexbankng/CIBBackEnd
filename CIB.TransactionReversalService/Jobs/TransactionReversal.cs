using CIB.TransactionReversalService.Entities;
using CIB.TransactionReversalService.Modules.Common;
using CIB.TransactionReversalService.Modules.Common.Interface;
using CIB.TransactionReversalService.Services;
using CIB.TransactionReversalService.Services.Request;
using CIB.TransactionReversalService.Services.Response;
using CIB.TransactionReversalService.Utils;
using Newtonsoft.Json;

namespace CIB.TransactionReversalService.Jobs;
public class TransactionReversal : ITransactionReversal
{
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<TransactionReversal> _logger;
  public TransactionReversal(IServiceProvider serviceProvider, ILogger<TransactionReversal> logger)
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
      string prallexBankCode = config.GetValue<string>("ParralexBankCode");
      int retryCount = int.Parse(config.GetValue<string>("retryCount"));
      var toDay = DateTime.Now.Date;
      var pendingTransfers = unitOfWork.BulkPaymentLogRepo.GetPendingTransferItems(0, 50, retryCount, toDay);
      if (pendingTransfers.Count != 0)
      {
        foreach (var i in pendingTransfers)
        {
          var pendingCreditItemList = unitOfWork.BulkCreditLogRepo.GetFailedTransaction(i.Id, 2, 5, 100, toDay);
          if (pendingCreditItemList.Any())
          {
            foreach (var creditLog in pendingCreditItemList)
            {
              if (creditLog?.CreditBankCode == prallexBankCode)
              {
                var date = DateTime.Now;
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
                      TransactionReference = Transactions.Ref(),
                      TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
                      BeneficiaryAccountName = i?.DebitAccountName,
                      BeneficiaryAccountNumber = i?.DebitAccountNumber,
                      Amount = creditLog?.CreditAmount,
                      Narration = $"RVS|{creditLog.Narration}",
                    }
                  },
                };
                var checkBal = await apiService.GetCustomerDetailByAccountNumber(i.SuspenseAccountNumber);
                if (checkBal.ResponseCode != "00")
                {
                  _logger.LogError("SERVER ERROR {0}, {1}, {2}", JsonConvert.SerializeObject(checkBal.ResponseCode), JsonConvert.SerializeObject(checkBal.ResponseDescription), JsonConvert.SerializeObject(checkBal.RequestId));
                }
                else
                {
                  if (checkBal.AvailableBalance < creditLog?.CreditAmount)
                  {
                    // notify settlemet officer
                    _logger.LogError("Insufficient Balance!");
                  }
                  else
                  {
                    var result = await apiService.PostIntraBankTransfer(transfer);
                    if (result.ResponseCode != "00")
                    {
                      _logger.LogError("SERVER ERROR {0}, {1}", JsonConvert.SerializeObject(result.ResponseCode), JsonConvert.SerializeObject(result.ResponseDescription));
                    }
                    ReverseFromIntraBankSuspenseToSource(creditLog, i, result, unitOfWork, prallexBankCode);
                  }
                }
              }
              else
              {
                var reversalListItem = new List<InterbankReversalDto>
                {
                  new InterbankReversalDto
                  {
                    Amount = creditLog?.CreditAmount,
                    Narration = $"RVS|{creditLog?.Narration}",
                    TranType = "PRINCIPAL"
                  },
                  new InterbankReversalDto
                  {
                    Amount = creditLog?.Fee,
                    Narration = $"RVS|BCHG|{creditLog?.Narration}",
                    TranType = "FEE"
                  },
                  new InterbankReversalDto
                  {
                    Amount = creditLog?.Vat,
                    Narration = $"RVS|VCHG|{creditLog?.Narration}",
                    TranType = "VAT"
                  }
                };

                var checkBal = await apiService.GetCustomerDetailByAccountNumber(i.IntreBankSuspenseAccountNumber);
                if (checkBal.ResponseCode != "00")
                {
                  _logger.LogError("BALANCE ENQUIRE FAILED ERROR {0}, {1}, {2}", JsonConvert.SerializeObject(checkBal.ResponseCode), JsonConvert.SerializeObject(checkBal.ResponseDescription), JsonConvert.SerializeObject(checkBal.RequestId));
                }
                else
                {
                  var transactionAmount = reversalListItem.Sum(ctx => ctx.Amount);
                  if (checkBal.AvailableBalance < transactionAmount)
                  {
                    // notify settlemet officer
                    _logger.LogError("Insufficient Balance!");
                  }
                  else
                  {
                    foreach (var reversalItem in reversalListItem)
                    {
                      var tranRef = Transactions.Ref();
                      var date = DateTime.Now;
                      var transfer = new PostIntraBankTransaction
                      {
                        AccountToDebit = i.IntreBankSuspenseAccountNumber,
                        UserName = i.InitiatorUserName,
                        Channel = "2",
                        TransactionLocation = i.TransactionLocation,
                        IntraTransferDetails = new List<IntraTransferDetail>
                        {
                          new IntraTransferDetail
                          {
                            TransactionReference = tranRef,
                            TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
                            BeneficiaryAccountName = i?.DebitAccountName,
                            BeneficiaryAccountNumber = i?.DebitAccountNumber,
                            Amount = reversalItem.Amount,
                            Narration = reversalItem.Narration,
                          }
                        },
                      };

                      var result = await apiService.PostIntraBankTransfer(transfer);
                      if (result.ResponseCode != "00")
                      {
                        _logger.LogError("FAIL TRANSACTION ERROR {0}, {1}", JsonConvert.SerializeObject(result.ResponseCode), JsonConvert.SerializeObject(result.ResponseDescription));
                        unitOfWork.TransactionRepo.Add(new TblTransaction
                        {
                          Id = Guid.NewGuid(),
                          TranAmout = reversalItem?.Amount,
                          TranDate = DateTime.Now,
                          SourceAccountNo = i.IntreBankSuspenseAccountNumber,
                          SourceAccountName = i.IntreBankSuspenseAccountName,
                          SourceBank = prallexBankCode,
                          TransactionStatus = nameof(TransactionStatus.Failed),
                          TranType = "Intrabank transfer",
                          Narration = $"{reversalItem?.Narration}",
                          Channel = "2",
                          DestinationAcctNo = i.DebitAccountNumber,
                          DestinationAcctName = i.DebitAccountName,
                          CorporateCustomerId = creditLog?.CorporateCustomerId,
                          BatchId = creditLog?.BatchId,
                          SessionId = result.TransactionReference,
                          TransactionReference = tranRef,
                        });
                        unitOfWork.Complete();
                      }
                      else
                      {
                        if (reversalItem?.TranType == "PRINCIPAL")
                        {
                          creditLog.CreditStatus = 4;
                          unitOfWork.BulkCreditLogRepo.Add(new TblNipbulkCreditLog
                          {
                            Id = Guid.NewGuid(),
                            TranLogId = creditLog?.TranLogId,
                            CreditAccountNumber = i.DebitAccountNumber,
                            CreditAccountName = i.DebitAccountName,
                            CreditAmount = Convert.ToDecimal(creditLog?.CreditAmount),
                            CreditBankCode = prallexBankCode,
                            CreditBankName = "Parralex Bank",
                            Narration = $"{reversalItem.Narration}",
                            CreditStatus = 1,
                            BatchId = creditLog?.BatchId,
                            ResponseCode = result.ResponseCode,
                            ResponseMessage = "REVERSED",
                            NameEnquiryStatus = 1,
                            TryCount = 0,
                            CorporateCustomerId = creditLog?.CorporateCustomerId,
                            CreditReversalId = creditLog?.Id,
                            CreditDate = DateTime.Now,
                            SessionId = result.TransactionReference,
                            TransactionReference = tranRef
                          });
                          unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
                        }
                        unitOfWork.TransactionRepo.Add(new TblTransaction
                        {
                          Id = Guid.NewGuid(),
                          TranAmout = reversalItem?.Amount,
                          TranDate = DateTime.Now,
                          SourceAccountNo = i.IntreBankSuspenseAccountNumber,
                          SourceAccountName = i.IntreBankSuspenseAccountName,
                          SourceBank = prallexBankCode,
                          TransactionStatus = nameof(TransactionStatus.Failed),
                          TranType = "Intrabank transfer",
                          Narration = $"{reversalItem?.Narration}",
                          Channel = "2",
                          DestinationAcctNo = i.DebitAccountNumber,
                          DestinationAcctName = i.DebitAccountName,
                          CorporateCustomerId = creditLog?.CorporateCustomerId,
                          BatchId = creditLog?.BatchId,
                          SessionId = result.TransactionReference,
                          TransactionReference = tranRef
                        });
                        unitOfWork.Complete();
                      }
                    }
                  }
                }
              }
            }
          }
          else
          {
            i.TransactionStatus = 1;
            unitOfWork.BulkPaymentLogRepo.UpdateStatus(i);
            unitOfWork.Complete();
          }
        }
      }
    }
    catch (AggregateException ex)
    {
      _logger.LogError("SERVER ERROR {0}, {1}, {2}", JsonConvert.SerializeObject(ex.StackTrace), JsonConvert.SerializeObject(ex.Source), JsonConvert.SerializeObject(ex.Message));
    }
    catch (Exception ex)
    {
      _logger.LogError("SERVER ERROR {0}, {1}, {2}", JsonConvert.SerializeObject(ex.StackTrace), JsonConvert.SerializeObject(ex.Source), JsonConvert.SerializeObject(ex.Message));
    }
  }
  private static void ReverseFromIntraBankSuspenseToSource(TblNipbulkCreditLog creditLog, TblNipbulkTransferLog info, TransferResponse result, IUnitOfWork unitOfWork, string bankCode)
  {
    if (result.ResponseCode != "00")
    {
      var transaction = new TblTransaction
      {
        Id = Guid.NewGuid(),
        TransactionReference = result.TransactionReference,
        TranAmout = creditLog?.CreditAmount,
        TranDate = DateTime.Now,
        SourceAccountNo = info.SuspenseAccountNumber,
        SourceAccountName = info.SuspenseAccountName,
        SourceBank = bankCode,
        TransactionStatus = nameof(TransactionStatus.Failed),
        TranType = "Intrabank transfer",
        Narration = $"RVS|{creditLog?.Narration}",
        Channel = "2",
        DesctionationBank = "Parrallex Bank",
        DestinationAcctNo = info.DebitAccountNumber,
        DestinationAcctName = info.DebitAccountName,
        CorporateCustomerId = creditLog?.CorporateCustomerId,
        BatchId = creditLog?.BatchId
      };
      creditLog.TryCount = (creditLog.TryCount ?? 0) + 1;
      creditLog.CreditStatus = 2;
      creditLog.ResponseMessage = result.ResponseDescription;
      creditLog.ResponseCode = result.ResponseCode;
      unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
      unitOfWork.TransactionRepo.Add(transaction);
      unitOfWork.Complete();
    }
    else
    {
      var transaction = new TblTransaction
      {
        Id = Guid.NewGuid(),
        TransactionReference = result.TransactionReference,
        TranAmout = creditLog?.CreditAmount,
        TranDate = DateTime.Now,
        SourceAccountNo = info.SuspenseAccountNumber,
        SourceAccountName = info.SuspenseAccountName,
        SourceBank = bankCode,
        TransactionStatus = nameof(TransactionStatus.Reversed),
        TranType = "Intrabank transfer",
        Narration = $"RVS|{creditLog?.Narration}",
        Channel = "WEB",
        DestinationAcctNo = info.DebitAccountNumber,
        DestinationAcctName = info.DebitAccountName,
        CorporateCustomerId = creditLog?.CorporateCustomerId,
        BatchId = creditLog?.BatchId
      };
      var reversal = new TblNipbulkCreditLog
      {
        Id = Guid.NewGuid(),
        TranLogId = creditLog?.TranLogId,
        CreditAccountNumber = info.DebitAccountNumber,
        CreditAccountName = info.DebitAccountName,
        CreditAmount = Convert.ToDecimal(creditLog?.CreditAmount),
        CreditBankCode = bankCode,
        CreditBankName = "Parralex Bank",
        Narration = $"RVS|{creditLog?.Narration}",
        CreditStatus = 1,
        BatchId = creditLog?.BatchId,
        TransactionReference = result.TransactionReference,
        ResponseCode = result.ResponseCode,
        ResponseMessage = result.ResponseDescription,
        NameEnquiryStatus = 1,
        TryCount = 0,
        CorporateCustomerId = creditLog?.CorporateCustomerId,
        CreditReversalId = creditLog?.Id,
        CreditDate = DateTime.Now
      };
      creditLog.CreditStatus = 4;
      unitOfWork.BulkCreditLogRepo.UpdateCreditStatus(creditLog);
      unitOfWork.TransactionRepo.Add(transaction);
      unitOfWork.BulkCreditLogRepo.Add(reversal);
      unitOfWork.Complete();
    }
  }
}
