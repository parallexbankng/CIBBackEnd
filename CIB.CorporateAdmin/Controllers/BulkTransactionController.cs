using AutoMapper;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.BulkTransaction.Dto;
using CIB.Core.Modules.BulkTransaction.Validation;
using CIB.Core.Modules.Transaction.Dto;
using CIB.Core.Modules.Transaction.Dto.Intrabank;
using CIB.Core.Services.Api;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using CIB.Core.Services._2FA;
using CIB.Core.Common.Dto;
using CIB.Core.Common;
using CIB.Core.Services.Notification;
using CIB.Core.Services.Api.Dto;
using CIB.CorporateAdmin.Utils;
using Newtonsoft.Json;
using CIB.Core.Services.Authentication;
using CIB.Core.Exceptions;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class BulkTransactionController : BaseAPIController
	{
		private readonly IApiService _apiService;
		private readonly IEmailService _emailService;
		private readonly IFileService _fileService;
		private readonly IConfiguration _config;
		private readonly IToken2faService _2FaService;
		private readonly INotificationService _notify;
		private readonly ILogger<BulkTransactionController> _logger;
		public BulkTransactionController(
			INotificationService notify,
			ILogger<BulkTransactionController> logger,
			IApiService apiService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			IHttpContextAccessor accessor,
			IEmailService emailService,
			IFileService fileService,
			IToken2faService token2FaService,
			IConfiguration config, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
		{
			_apiService = apiService;
			_emailService = emailService;
			_fileService = fileService;
			_config = config;
			_2FaService = token2FaService;
			_logger = logger;
			this._notify = notify;
		}
		//<summary>
		// bulk payment Verify
		//</summary>
		//<param name="model"> bulk payment parameters</param>
		//<returns>returns InterbankBulkResponsedata</returns>
		[HttpPost("VerifyBulkTransfer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<ResponseDTO<VerifyBulkTransactionResponse>>> VerifyBulkTransfer([FromForm] VerifyBulkTransactionDto model)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}


				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
				var parallexBank = _config.GetValue<string>("ParralexBank");
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (corporateCustomer == null || CorporateProfile.CorporateCustomerId != corporateCustomer.Id)
				{
					return BadRequest("UnAuthorized Access");
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
				{
					if (Enum.TryParse(corporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
					{
						if (authorizationType != AuthorizationType.Single_Signatory)
						{
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(corporateCustomer.CustomerId), "");
						return BadRequest("Authorization type could not be determined!!!");
					}
				}
				var payload = new VerifyBulkTransaction
				{
					SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
					Narration = Encryption.DecryptStrings(model.Narration),
					WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
					Currency = Encryption.DecryptStrings(model.Currency)
				};
				var validator = new VerifyBulkTransactionValidator();
				var results = await validator.ValidateAsync(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}
				var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);
				if (senderInfo.ResponseCode != "00")
				{
					var message = $"Source account number could not be verified -> {senderInfo.ResponseDescription}";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}
				if (senderInfo.AccountStatus != "A")
				{
					var message = $"Source account is not active transaction cannot be completed ";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}

				if (senderInfo.FreezeCode != "N")
				{
					var message = $"Source account is On Debit Freeze";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}

				if (senderInfo.CurrencyCode != "NGN")
				{
					var message = $"Only naira Transaction is allow , transaction can not be completed";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}


				var dtb = _fileService.ReadExcelFile(model.files);
				if (dtb.Count == 0)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", "No Data in Excel File To Read", JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest("No Data in Excel File To Read");
				}

				var bulkTransactionItems = new List<VerifyBulkTransactionResponseDto>();
				var response = new VerifyBulkTransactionResponse();
				var bankList = await _apiService.GetBanks();
				if (bankList.ResponseCode != "00")
				{
					return BadRequest(bankList.ResponseMessage);
				}
				decimal totalAmount = 0;
				decimal totalFeeCharges = 0;
				decimal totalVatsCharger = 0;
				var feeCharges = await UnitOfWork.NipsFeeChargeRepo.ListAllAsync();
				await Task.WhenAll(dtb.AsEnumerable().Select(async row =>
				{
					var errorMsg = "";
					if (string.IsNullOrEmpty(row.BankCode) || string.IsNullOrEmpty(row.BankCode?.Trim()))
					{
						errorMsg = "Bank code is empty;";
					}
					if (row.BankCode != null && row.BankCode.Length != 6)
					{
						errorMsg += "Invalid Bank code";
					}

					if (string.IsNullOrEmpty(row.CreditAccount) || string.IsNullOrEmpty(row.CreditAccount?.Trim()))
					{
						errorMsg += "Credit account number is empty;";
					}
					if (row.CreditAccount != null && row.CreditAccount.Length != 10)
					{
						errorMsg += "Credit account number is invalid ";
					}
					if (row.CreditAmount <= 0)
					{
						errorMsg += "Credit amount is invalid;";
					}
					if (corporateCustomer.MaxAccountLimit < row.CreditAmount)
					{
						errorMsg += $"Transaction amount {row.CreditAmount} has exceeded the maximum transaction amount {corporateCustomer.MaxAccountLimit} for your organisation";
					}
					if (string.IsNullOrEmpty(row.Narration) || string.IsNullOrEmpty(row.Narration?.Trim()))
					{
						errorMsg += "Narration is empty and this transaction will not be treated";
					}

					if (bulkTransactionItems.Count != 0)
					{
						var duplicateAccountNumber = bulkTransactionItems?.Where(xtc => xtc.CreditAccount == row.CreditAccount && xtc.CreditAmount == row.CreditAmount).ToList();
						if (duplicateAccountNumber.Count > 0)
						{
							errorMsg += $"duplicate account number:{row.CreditAccount} and amount:{row.CreditAmount}";
						}
					}

					if (string.IsNullOrEmpty(errorMsg) || errorMsg.Contains($"duplicate account number:{row.CreditAccount} and amount:{row.CreditAmount}"))
					{
						var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == row.BankCode);
						row.BankName = bank != null ? bank.InstitutionName : parallexBank;
						var info = await _apiService.BankNameInquire(row.CreditAccount, row.BankCode);
						if (info.ResponseCode != "00")
						{
							errorMsg += $"{info.ResponseMessage} -> {info.ResponseCode}";
						}
						else
						{
							row.AccountName = info.AccountName;
							if (row.BankCode == parallexBankCode)
							{
								totalAmount += row.CreditAmount;
							}
							else
							{
								var nipsCharge = NipsCharge.Calculate(feeCharges, row.CreditAmount);
								totalAmount += row.CreditAmount;
								totalFeeCharges += nipsCharge.Fee;
								totalVatsCharger += nipsCharge.Vat;
							}
						}
					}

					if (row.Narration != null && row.Narration.Length > 50)
					{
						errorMsg += "Narration should not be more than 50 characters; or it will be truncated ";
					}
					row.Error = errorMsg;
					bulkTransactionItems.Add(row);
				}));

				if (bulkTransactionItems.Count == 0)
				{
					var message = "Error Reading Excel File. There must be at least one valid entry";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(corporateCustomer.CustomerId), JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}
				if (totalAmount > corporateCustomer.BulkTransDailyLimit)
				{
					var message = $"Transaction amount {totalAmount} has exceeded the maximum daily Bulk transaction limit per Day {corporateCustomer.BulkTransDailyLimit} for your organisation";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(corporateCustomer.CustomerId), JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}
				if (corporateCustomer.MinAccountLimit > totalAmount)
				{
					var message = $"Transaction amount {totalAmount} is below the Minimum transaction amount  {corporateCustomer.MinAccountLimit} for your organisation";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest(message);
				}

				var validateRelatedAccount = await this.ValidateRelatedAccount(corporateCustomer, payload.SourceAccountNumber);
				if (!validateRelatedAccount.Status)
				{
					return BadRequest(validateRelatedAccount.Message);
				}
				if (DailyLimitExceeded(corporateCustomer, totalAmount, out string errorMessage))
				{
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", errorMessage, JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(errorMessage);
				}
				if (Enum.TryParse(corporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
				{
					if (_auth != AuthorizationType.Single_Signatory)
					{
						if (payload.WorkflowId == null)
						{
							LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", "Workflow is required", JsonConvert.SerializeObject(corporateCustomer.CustomerId));
							return BadRequest("Workflow is required");
						}
						var workflowStatus = ValidateWorkflowAccess(payload.WorkflowId, totalAmount);
						if (!workflowStatus.Status)
						{
							LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", workflowStatus.Message, JsonConvert.SerializeObject(corporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
							return BadRequest(workflowStatus.Message);
						}
					}
				}

				response.AuthLimit = corporateCustomer.AuthenticationLimit != null ? 1 : 0;
				response.AuthLimitIsEnable = totalAmount >= corporateCustomer.AuthenticationLimit ? 1 : 0;
				response.Fee = totalFeeCharges;
				response.Vat = totalVatsCharger;
				response.TotalAmount = totalAmount + totalFeeCharges + totalVatsCharger;
				response.Transaction = bulkTransactionItems.OrderByDescending(ctx => !string.IsNullOrEmpty(ctx.Error))?.ToList();
				return Ok(new ResponseDTO<VerifyBulkTransactionResponse>(_data: response, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("InitiateBulkTransfer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> InitiateBulkTransfer([FromForm] InitiateBulkTransactionDto model)
		{
			try
			{
				var parallexSuspenseAccount = _config.GetValue<string>("NIPSBulkSuspenseAccount");
				var parallexSuspenseAccountName = _config.GetValue<string>("NIPSBulkSuspenseAccountName");

				var parallexInterSuspenseAccount = _config.GetValue<string>("NIPSInterBulkSuspenseAccount");
				var parallexInterSuspenseAccountName = _config.GetValue<string>("NIPSInterBulkSuspenseAccountName");

				var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
				var parralexBank = _config.GetValue<string>("ParralexBank");
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}

				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				var tranlg = new TblNipbulkTransferLog();
				var bankNipBulkCreditLogList = new List<TblNipbulkCreditLog>();
				var logDuplicateTransaction = new List<VerifyBulkTransactionResponseDto>();
				var bulkSuspenseCreditItems = new List<TblNipbulkCreditLog>();
				var bulkSuspenseVatFee = new List<TblNipbulkCreditLog>();
				var batchId = Guid.NewGuid();

				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (tblCorporateCustomer == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
				{
					if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
					{
						if (_authType != AuthorizationType.Single_Signatory)
						{
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var payload = new InitiateBulkTransaction
				{
					SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
					Narration = Encryption.DecryptStrings(model.Narration),
					WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
					TransactionLocation = Encryption.DecryptStrings(model.TransactionLocation),
					Currency = Encryption.DecryptStrings(model.Currency),
					Otp = Encryption.DecryptStrings(model.Otp),
					Pin = Encryption.DecryptStrings(model.Pin),
					AuthLimitIsEnable = Encryption.DecryptInt(model.AuthLimitIsEnable),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					AllowDuplicateAccount = Encryption.DecryptBooleans(model.AllowDuplicateAccount)
				};

				var userName = $"{CorporateProfile.Username}";
				var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", $"2FA API ERROR : {validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(validOTP.ResponseMessage);
				}

				if (payload.AuthLimitIsEnable > 0)
				{
					var transPin = CorporateProfile.TranPin != null ? Encryption.DecriptPin(CorporateProfile.TranPin) : null;
					if (transPin == null)
					{
						return BadRequest("transaction can not be completed because transaction pin is not set");
					}
					else
					{
						if (transPin != payload.Pin)
						{
							LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", $"Invalid Pin", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Invalid Transaction pin");
						}
					}

				}

				//path to image folder
				var path = Path.Combine("wwwroot", "bulkupload", tblCorporateCustomer.CustomerId);
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}

				string fileName = tblCorporateCustomer.CustomerId + "-" + DateTime.Now.ToShortDateString().Replace("/", "-").Replace(",", "") + "-" + DateTime.Now.ToShortTimeString().Replace(" ", "-").Replace(",", "").Replace(":", "-") + $".xlsx";

				string filePath = Path.Combine(path, fileName);

				var uploadListItems = _fileService.ReadAndSaveExcelFile(model.files, filePath);

				var bankList = await _apiService.GetBanks();

				if (bankList.ResponseCode != "00")
				{
					return BadRequest(bankList.ResponseMessage);
				}

				if (uploadListItems.Count == 0)
				{
					_fileService.DeleteFile(fileName);
					return BadRequest("No Data in Excel File to read");
				}
				var totalDebitProcessAmount = uploadListItems.Sum(ctx => ctx.CreditAmount);

				if (totalDebitProcessAmount <= 0)
				{
					_fileService.DeleteFile(filePath);
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "invalid Debit Amount", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest("invalid Debit Amount");

				}

				if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
				{
					if (_auth != AuthorizationType.Single_Signatory)
					{
						if (payload.WorkflowId == null)
						{
							_fileService.DeleteFile(filePath);
							LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "Workflow is required", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Workflow is required");
						}
						var validateWorkFlow = ValidateWorkflowAccess(payload.WorkflowId, totalDebitProcessAmount);
						if (!validateWorkFlow.Status)
						{
							_fileService.DeleteFile(filePath);
							LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", validateWorkFlow.Message, JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
							return BadRequest(validateWorkFlow.Message);
						}
					}
				}
				else
				{
					_fileService.DeleteFile(filePath);
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest("Authorization type could not be determined!!!");
				}
				var validateRelatedAccount = await this.ValidateRelatedAccount(tblCorporateCustomer, payload.SourceAccountNumber);
				if (!validateRelatedAccount.Status)
				{
					return BadRequest(validateRelatedAccount.Message);
				}

				if (tblCorporateCustomer.MaxAccountLimit < totalDebitProcessAmount)
				{
					_fileService.DeleteFile(filePath);
					var message = $"Transaction amount {totalDebitProcessAmount} has exceeded the maximum transaction amount {tblCorporateCustomer.MaxAccountLimit} for your organisation";
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", message, JsonConvert.SerializeObject(payload.SourceAccountNumber), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}
				var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);
				if (senderInfo.ResponseCode != "00")
				{
					_fileService.DeleteFile(filePath);
					var message = $"Source account number could not be verified -> {senderInfo.ResponseDescription}";
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}
				if (senderInfo.AccountStatus != "A")
				{
					_fileService.DeleteFile(filePath);
					var message = $"Source account is not active transaction cannot be completed ";
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}
				if (senderInfo.CurrencyCode != "NGN")
				{
					var message = $"Only naira Transaction is allow , transaction can not be completed";
					LogFormater<BulkTransactionController>.Error(_logger, "VerifyBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(message);
				}

				tranlg.Id = Guid.NewGuid();
				tranlg.CompanyId = tblCorporateCustomer.Id;
				tranlg.InitiatorId = CorporateProfile.Id;
				tranlg.DebitAccountName = senderInfo.AccountName;
				tranlg.DebitAccountNumber = payload.SourceAccountNumber;
				tranlg.Narration = $"BP|{batchId}|{payload.Narration}|{tblCorporateCustomer.CompanyName}";
				tranlg.DateInitiated = DateTime.Now;
				tranlg.PostingType = "Bulk";
				tranlg.Currency = payload.Currency;
				tranlg.TransactionStatus = 0;
				tranlg.TryCount = 0;
				tranlg.TransferType = nameof(TransactionType.Bulk_Transfer);
				tranlg.BatchId = batchId;
				tranlg.BulkFileName = fileName;
				tranlg.BulkFilePath = filePath;
				tranlg.TransactionStatus = 0;
				tranlg.ApprovalStatus = 0;
				tranlg.ApprovalStage = 1;
				tranlg.InitiatorUserName = CorporateProfile.Username;
				tranlg.TransactionLocation = payload.TransactionLocation;
				tranlg.SuspenseAccountName = parallexSuspenseAccountName;
				tranlg.SuspenseAccountNumber = parallexSuspenseAccount;
				tranlg.IntreBankSuspenseAccountName = parallexInterSuspenseAccountName;
				tranlg.IntreBankSuspenseAccountNumber = parallexInterSuspenseAccount;
				tranlg.Sn = 0;
				tranlg.TotalCredits = 0;
				tranlg.NoOfCredits = 0;
				tranlg.InterBankTryCount = 0;
				tranlg.InterBankTotalCredits = 0;
				tranlg.Status = 0;
				tranlg.TransactionReference = Generate16DigitNumber.Create16DigitString();
				tranlg.ServerIp = Helper.GetHostIp();

				var feeCharges = await UnitOfWork.NipsFeeChargeRepo.ListAllAsync();
				await Task.WhenAll(uploadListItems.AsEnumerable().Select(async row =>
				{
					string errorMsg = "";
					if (string.IsNullOrEmpty(row.BankCode) || string.IsNullOrEmpty(row.BankCode?.Trim()))
					{
						errorMsg = "invalid Bank code";
					}
					if (row.BankCode != null && row.BankCode.Length != 6)
					{
						errorMsg += "invalid Bank code";
					}

					if (string.IsNullOrEmpty(row.CreditAccount) || string.IsNullOrEmpty(row.CreditAccount?.Trim()))
					{
						errorMsg += "invalid credit account number";
					}
					if (row.CreditAccount != null && row.CreditAccount.Length != 10)
					{
						errorMsg += "invalid credit account number";
					}
					if (row.CreditAmount <= 0)
					{
						errorMsg += "invalid Credit amount;";
					}
					if (string.IsNullOrEmpty(row.Narration) || string.IsNullOrEmpty(row.Narration?.Trim()))
					{
						errorMsg += "Narration is empty;";
					}
					if (tblCorporateCustomer.MaxAccountLimit < row.CreditAmount)
					{
						errorMsg += $"Transaction amount {row.CreditAmount} has exceeded the maximum transaction amount {tblCorporateCustomer.MaxAccountLimit} for your organisation";
					}

					var duplicateError = errorMsg.Contains($"this Account Number {row.CreditAccount} Already Exist");
					if (string.IsNullOrEmpty(errorMsg) || duplicateError == true)
					{
						var customerInfo = await _apiService.BankNameInquire(row.CreditAccount, row.BankCode);
						var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == row.BankCode);
						var bankName = bank != null ? bank.InstitutionName : parralexBank;
						var nipsCharge = row.BankCode != parallexBankCode ? NipsCharge.Calculate(feeCharges, row.CreditAmount) : NipsCharge.Calculate(feeCharges, 0);
						var myNarratione = row.Narration.Length > 50 ? row.Narration[..50] : row.Narration;
						var narration = $"IP|{batchId}|{tblCorporateCustomer.CompanyName}|{customerInfo.AccountName}|{myNarratione}";
						if (bankNipBulkCreditLogList.Count > 0)
						{
							var duplicateAccountNumber = bankNipBulkCreditLogList?.Where(xtc => xtc.CreditAccountNumber == row.CreditAccount && xtc.CreditAmount == row.CreditAmount).ToList();
							if (duplicateAccountNumber.Count != 0)
							{
								errorMsg += $"Account Number {row.CreditAccount} Already Exist";
								row.BankName = bankName;
								logDuplicateTransaction.Add(row);
							}
						}

						if (customerInfo.ResponseCode != "00")
						{
							var nipCreditInfo = new TblNipbulkCreditLog
							{
								Id = Guid.NewGuid(),
								TranLogId = tranlg.Id,
								CreditAccountNumber = row.CreditAccount,
								CreditAccountName = customerInfo.AccountName,
								CreditAmount = Convert.ToDecimal(row.CreditAmount),
								CreditBankCode = row.BankCode,
								CreditBankName = bankName,
								Narration = narration,
								CreditStatus = 2,
								BatchId = batchId,
								NameEnquiryRef = customerInfo.RequestId,
								ResponseCode = customerInfo.ResponseCode,
								ResponseMessage = customerInfo.ResponseMessage,
								NameEnquiryStatus = 0,
								TryCount = 0,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								InitiateDate = DateTime.Now,
								Fee = row.BankCode != parallexBankCode ? nipsCharge.Fee : 0,
								Vat = row.BankCode != parallexBankCode ? nipsCharge.Vat : 0,
								ServerIp = tranlg.ServerIp,
								TransactionReference = Generate16DigitNumber.Create16DigitString(),
							};
							bankNipBulkCreditLogList.Add(nipCreditInfo);
						}
						else
						{
							if (tblCorporateCustomer.MaxAccountLimit < row.CreditAmount)
							{
								var nipCreditInfos = new TblNipbulkCreditLog
								{
									Id = Guid.NewGuid(),
									TranLogId = tranlg.Id,
									CreditAccountNumber = row.CreditAccount,
									CreditAccountName = customerInfo.AccountName,
									CreditAmount = Convert.ToDecimal(row.CreditAmount),
									CreditBankCode = row.BankCode,
									CreditBankName = bankName,
									Narration = narration,
									CreditStatus = 2,
									BatchId = batchId,
									BankVerificationNo = customerInfo.BVN,
									KycLevel = customerInfo.KYCLevel,
									NameEnquiryRef = customerInfo.RequestId,
									ResponseCode = customerInfo.ResponseCode,
									ResponseMessage = errorMsg,
									NameEnquiryStatus = 0,
									TryCount = 0,
									CorporateCustomerId = CorporateProfile.CorporateCustomerId,
									InitiateDate = DateTime.Now,
									ServerIp = tranlg.ServerIp,
									Fee = row.BankCode != parallexBankCode ? nipsCharge.Fee : 0,
									Vat = row.BankCode != parallexBankCode ? nipsCharge.Vat : 0,
									TransactionReference = Generate16DigitNumber.Create16DigitString(),
								};
								bankNipBulkCreditLogList.Add(nipCreditInfos);
							}
							else
							{
								var nipCreditInfos = new TblNipbulkCreditLog
								{
									Id = Guid.NewGuid(),
									TranLogId = tranlg.Id,
									CreditAccountNumber = row.CreditAccount,
									CreditAccountName = customerInfo.AccountName,
									CreditAmount = Convert.ToDecimal(row.CreditAmount),
									CreditBankCode = row.BankCode,
									CreditBankName = bankName,
									Narration = narration,
									CreditStatus = 0,
									BatchId = batchId,
									BankVerificationNo = customerInfo.BVN,
									KycLevel = customerInfo.KYCLevel,
									NameEnquiryRef = customerInfo.RequestId,
									ResponseCode = customerInfo.ResponseCode,
									ResponseMessage = customerInfo.ResponseMessage,
									NameEnquiryStatus = 1,
									TryCount = 0,
									CorporateCustomerId = CorporateProfile.CorporateCustomerId,
									InitiateDate = DateTime.Now,
									ServerIp = tranlg.ServerIp,
									Fee = row.BankCode != parallexBankCode ? nipsCharge.Fee : 0,
									Vat = row.BankCode != parallexBankCode ? nipsCharge.Vat : 0,
									TransactionReference = Generate16DigitNumber.Create16DigitString(),
								};
								bankNipBulkCreditLogList.Add(nipCreditInfos);
							}
						}
					}
				}));

				decimal? totalDebitAmountWithOutCharges = bankNipBulkCreditLogList.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
				decimal? interBankTotalDebitAmount = bankNipBulkCreditLogList.Where(ctx => ctx.NameEnquiryStatus == 1 && ctx.CreditBankCode != parallexBankCode).Sum(ctx => ctx.CreditAmount);
				decimal? intraBankTotalDebitAmount = bankNipBulkCreditLogList.Where(ctx => ctx.NameEnquiryStatus == 1 && ctx.CreditBankCode == parallexBankCode).Sum(ctx => ctx.CreditAmount);
				var interBankCreditItems = bankNipBulkCreditLogList.Where(ctx => ctx.CreditBankCode != parallexBankCode).ToList();
				var intraBankCreditItems = bankNipBulkCreditLogList.Where(ctx => ctx.CreditBankCode == parallexBankCode).ToList();

				if (interBankCreditItems.Any())
				{
					decimal? totalVat = bankNipBulkCreditLogList.Where(ctx => ctx.CreditBankCode != parallexBankCode && ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.Vat);
					decimal? totalFee = bankNipBulkCreditLogList.Where(ctx => ctx.CreditBankCode != parallexBankCode && ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.Fee);
					bulkSuspenseCreditItems.Add(new TblNipbulkCreditLog
					{
						Id = Guid.NewGuid(),
						TranLogId = tranlg.Id,
						CreditAccountNumber = parallexInterSuspenseAccount,
						CreditAccountName = parallexInterSuspenseAccountName,
						CreditAmount = Convert.ToDecimal(interBankTotalDebitAmount),
						CreditBankCode = parallexBankCode,
						CreditBankName = parralexBank,
						Narration = tranlg.Narration,
						CreditStatus = 2,
						BatchId = batchId,
						NameEnquiryStatus = 0,
						TryCount = 0,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						CreditDate = DateTime.Now,
					});
					bulkSuspenseVatFee.Add(new TblNipbulkCreditLog
					{
						Id = Guid.NewGuid(),
						TranLogId = tranlg.Id,
						CreditAccountNumber = parallexInterSuspenseAccount,
						CreditAccountName = parallexInterSuspenseAccountName,
						CreditAmount = Convert.ToDecimal(totalVat),
						CreditBankCode = parallexBankCode,
						CreditBankName = parralexBank,
						Narration = $"VCHG|{tranlg.Narration}",
						CreditStatus = 2,
						BatchId = batchId,
						NameEnquiryStatus = 0,
						TransactionReference = Generate16DigitNumber.Create16DigitString(),
						TryCount = 0,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						CreditDate = DateTime.Now,
					});
					bulkSuspenseVatFee.Add(new TblNipbulkCreditLog
					{
						Id = Guid.NewGuid(),
						TranLogId = tranlg.Id,
						CreditAccountNumber = parallexInterSuspenseAccount,
						CreditAccountName = parallexInterSuspenseAccountName,
						CreditAmount = Convert.ToDecimal(totalFee),
						CreditBankCode = parallexBankCode,
						CreditBankName = parralexBank,
						Narration = $"BCHG|{tranlg.Narration}",
						CreditStatus = 2,
						BatchId = batchId,
						NameEnquiryStatus = 0,
						TransactionReference = Generate16DigitNumber.Create16DigitString(),
						TryCount = 0,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						CreditDate = DateTime.Now,
					});
					tranlg.InterBankStatus = 0;
					tranlg.TotalFee = totalFee;
					tranlg.TotalVat = totalVat;
					tranlg.InterBankTotalAmount = interBankTotalDebitAmount;
				}

				if (intraBankCreditItems.Any())
				{
					bulkSuspenseCreditItems.Add(new TblNipbulkCreditLog
					{
						Id = Guid.NewGuid(),
						TranLogId = tranlg.Id,
						CreditAccountNumber = parallexSuspenseAccount,
						CreditAccountName = parallexSuspenseAccountName,
						CreditAmount = Convert.ToDecimal(intraBankTotalDebitAmount),
						CreditBankCode = parallexBankCode,
						CreditBankName = parralexBank,
						Narration = tranlg.Narration,
						CreditStatus = 2,
						BatchId = batchId,
						NameEnquiryStatus = 0,
						TryCount = 0,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						CreditDate = DateTime.Now,
					});
					tranlg.IntraBankTotalAmount = intraBankTotalDebitAmount;
					tranlg.IntraBankStatus = 0;
				}

				decimal? totalDebitAmount = 0;
				if (interBankCreditItems.Any())
				{
					totalDebitAmount = totalDebitAmountWithOutCharges + tranlg.TotalFee + tranlg.TotalVat;
				}
				else
				{
					totalDebitAmount = totalDebitAmountWithOutCharges;
				}

				tranlg.DebitAmount = totalDebitAmountWithOutCharges;
				tranlg.NoOfCredits = bankNipBulkCreditLogList.Count(ctx => ctx.NameEnquiryStatus == 1);
				if (totalDebitAmount < 1)
				{
					return BadRequest($"Transaction  amount is less than 1");
				}

				if (logDuplicateTransaction.Any())
				{
					var auditTrail = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer_With_Duplicate).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated transfer of " + tranlg.DebitAmount + "from " + payload.SourceAccountNumber + " With Duplicate Account Numbers: " + " " + Formater.JsonType(logDuplicateTransaction),
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Corporate User Initiated Bulk transfer with duplicate Account Number",
						TimeStamp = DateTime.Now
					};
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.Complete();
				}

				if (DailyLimitExceeded(tblCorporateCustomer, (decimal)totalDebitAmount, out string errorMsg))
				{
					LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", errorMsg, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
					return BadRequest(errorMsg);
				}

				var tranDate = DateTime.Now;
				var tblCorporateApprovalHistories = new List<TblCorporateApprovalHistory>();
				var date = DateTime.Now;

				if (_auth == AuthorizationType.Single_Signatory)
				{
					if (senderInfo.Effectiveavail < totalDebitAmount)
					{
						_fileService.DeleteFile(filePath);
						LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "Insufficient funds", JsonConvert.SerializeObject(payload.SourceAccountNumber), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
						return BadRequest("Insufficient funds");
					}

					if (bulkSuspenseCreditItems.Count > 2)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "The transaction is not balanced.", JsonConvert.SerializeObject(bulkSuspenseCreditItems), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), Helper.GetDivce(UserAgent));
						return BadRequest("The transaction is not balanced.");
					}

					var postBulkTransaction = FormatBulkTransaction(bulkSuspenseCreditItems, tranlg.DebitAmount, tranlg);
					var postBulkIntraBankBulk = await _apiService.IntraBankBulkTransfer(postBulkTransaction);

					if (postBulkIntraBankBulk.ResponseCode != "00")
					{
						_logger.LogError("TRANSACTION ERROR {0}, {1}, {2}", Formater.JsonType(postBulkIntraBankBulk.ResponseCode), Formater.JsonType(postBulkIntraBankBulk.ResponseMessage), Formater.JsonType(postBulkIntraBankBulk.ErrorDetail));
						if (interBankCreditItems.Any())
						{
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = tranlg.InterBankTotalAmount,
								DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
								DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Failed),
								Narration = $"{tranlg.Narration}|inter",
								SourceAccountName = senderInfo.AccountName,
								SourceAccountNo = senderInfo.AccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = tranlg.TransactionReference,
								SessionId = postBulkIntraBankBulk.TrnId,
								TranDate = tranDate,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = batchId
							});
						}
						if (intraBankCreditItems.Any())
						{
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = tranlg.IntraBankTotalAmount,
								DestinationAcctName = tranlg.SuspenseAccountName,
								DestinationAcctNo = tranlg.SuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Failed),
								Narration = $"{tranlg.Narration}|intra",
								SourceAccountName = senderInfo.AccountName,
								SourceAccountNo = senderInfo.AccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = tranlg.TransactionReference,
								SessionId = postBulkIntraBankBulk.TrnId,
								TranDate = tranDate,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = batchId
							});
						}
						var bulkTranLogAuditTrail = new TblAuditTrail
						{
							Id = Guid.NewGuid(),
							ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
							Ipaddress = payload.IPAddress,
							Macaddress = payload.MACAddress,
							HostName = payload.HostName,
							ClientStaffIpaddress = payload.ClientStaffIPAddress,
							NewFieldValue = "Corporate User: " + CorporateProfile.FullName + " Initiated transfer of " + tranlg.DebitAmount + " from " + payload.SourceAccountNumber,
							PreviousFieldValue = "",
							TransactionId = tranlg.TransactionReference,
							UserId = CorporateProfile.Id,
							Username = CorporateProfile.Username,
							Description = "Corporate User Initiated Bulk transfer Failed, this is process with bulk transaction API",
							TimeStamp = DateTime.Now
						};
						tranlg.ResponseCode = postBulkIntraBankBulk.ResponseCode;
						tranlg.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
						tranlg.ErrorDetail = Formater.JsonType(postBulkIntraBankBulk.ErrorDetail);
						tranlg.Status = 2;
						tranlg.TransactionStatus = 2;
						tranlg.ApprovalStatus = 1;
						tranlg.SessionId = postBulkIntraBankBulk.TrnId;
						UnitOfWork.AuditTrialRepo.Add(bulkTranLogAuditTrail);
						UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
						UnitOfWork.Complete();
						return BadRequest($"Transaction can not be completed at the moment -> {postBulkIntraBankBulk.ResponseMessage}:{postBulkIntraBankBulk.ResponseCode}");
					}
					if (interBankCreditItems.Count != 0)
					{
						var postVatResult = await FormatBulkTransactionCharges(bulkSuspenseVatFee, tranlg);
						var checkFeeTransaction = postVatResult.Where(ctx => ctx.ResponseCode != "00").ToList();
						if (checkFeeTransaction.Count == 0)
						{
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = tranlg.InterBankTotalAmount,
								DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
								DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Successful),
								Narration = $"{tranlg.Narration}|inter",
								SourceAccountName = senderInfo.AccountName,
								SourceAccountNo = senderInfo.AccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = tranlg.TransactionReference,
								SessionId = postBulkIntraBankBulk.TrnId,
								TranDate = DateTime.Now,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = batchId
							});
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = tranlg.TotalVat,
								DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
								DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Successful),
								Narration = $"VCHG|{tranlg.Narration}",
								SourceAccountName = senderInfo.AccountName,
								SourceAccountNo = senderInfo.AccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = bulkSuspenseVatFee[0].TransactionReference,
								SessionId = postVatResult[0].TransactionReference,
								TranDate = tranDate,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = batchId
							});
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = tranlg.TotalFee,
								DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
								DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Successful),
								Narration = $"BCHG|{tranlg.Narration}",
								SourceAccountName = senderInfo.AccountName,
								SourceAccountNo = senderInfo.AccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = bulkSuspenseVatFee[1].TransactionReference,
								SessionId = postVatResult[1].TransactionReference,
								TranDate = tranDate,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = batchId
							});
							UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail
							{
								Id = Guid.NewGuid(),
								ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
								Ipaddress = payload.IPAddress,
								Macaddress = payload.MACAddress,
								HostName = payload.HostName,
								ClientStaffIpaddress = payload.ClientStaffIPAddress,
								NewFieldValue = "Corporate User: " + CorporateProfile.FullName + " Nip Charges of " + tranlg.TotalFee + " from " + payload.SourceAccountNumber,
								PreviousFieldValue = "",
								TransactionId = bulkSuspenseVatFee[0].TransactionReference,
								UserId = CorporateProfile.Id,
								Username = CorporateProfile.Username,
								Description = "Corporate Bulk transfer Nip Charges",
								TimeStamp = DateTime.Now
							});
							UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail
							{
								Id = Guid.NewGuid(),
								ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
								Ipaddress = payload.IPAddress,
								Macaddress = payload.MACAddress,
								HostName = payload.HostName,
								ClientStaffIpaddress = payload.ClientStaffIPAddress,
								NewFieldValue = "Corporate User: " + CorporateProfile.FullName + " Vat Charges of " + tranlg.TotalVat + " from " + payload.SourceAccountNumber,
								PreviousFieldValue = "",
								TransactionId = bulkSuspenseVatFee[1].TransactionReference,
								UserId = CorporateProfile.Id,
								Username = CorporateProfile.Username,
								Description = "Corporate Bulk transfer Vat Charges",
								TimeStamp = DateTime.Now
							});
						}
						else
						{
							LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "Bulk Inter bank fees and vats couldnot be posted", JsonConvert.SerializeObject(checkFeeTransaction), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						}

					}
					if (intraBankCreditItems.Count != 0)
					{
						UnitOfWork.TransactionRepo.Add(new TblTransaction
						{
							Id = Guid.NewGuid(),
							TranAmout = tranlg.IntraBankTotalAmount,
							DestinationAcctName = tranlg.SuspenseAccountName,
							DestinationAcctNo = tranlg.SuspenseAccountNumber,
							DesctionationBank = parralexBank,
							TranType = "bulk",
							TransactionStatus = nameof(TransactionStatus.Successful),
							Narration = $"{tranlg.Narration}|intra",
							SourceAccountName = senderInfo.AccountName,
							SourceAccountNo = senderInfo.AccountNumber,
							SourceBank = parralexBank,
							CustAuthId = CorporateProfile.Id,
							Channel = "WEB",
							TransactionReference = tranlg.TransactionReference,
							SessionId = postBulkIntraBankBulk.TrnId,
							TranDate = tranDate,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							BatchId = batchId
						});
					}
					var auditTrail = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated/Approved transfer of " + tranlg.DebitAmount + "from " + payload.SourceAccountNumber,
						PreviousFieldValue = "",
						//TransactionId = postBulkIntraBankBulk.RequestId,
						TransactionId = tranlg.TransactionReference,
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Corporate User Initiated/Approved Bulk transfer,  this is process with bulk transaction API",
						TimeStamp = DateTime.Now
					};
					tranlg.ResponseCode = postBulkIntraBankBulk.ResponseCode;
					tranlg.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
					tranlg.Status = 1;
					tranlg.DateProccessed = date;
					tranlg.ApprovalStatus = 1;
					tranlg.ApprovalCount = 1;
					tranlg.ApprovalStage = 1;
					tranlg.TransactionStatus = 0;
					tranlg.SessionId = postBulkIntraBankBulk.TrnId;
					UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.NipBulkCreditLogRepo.AddRange(bankNipBulkCreditLogList);
					UnitOfWork.Complete();
					return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful" });
				}
				else
				{
					var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(payload.WorkflowId.Value);
					if (workflowHierarchies.Count == 0)
					{
						_fileService.DeleteFile(filePath);
						LogFormater<BulkTransactionController>.Error(_logger, "InitiateBulkTransfer", "Authorizer has not been set", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorizer has not been set");
					}
					var auditTrail = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated transfer of " + tranlg.DebitAmount + "from " + payload.SourceAccountNumber,
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Corporate User Initiated Bulk transfer",
						TimeStamp = DateTime.Now
					};
					foreach (var item in workflowHierarchies)
					{
						var toApproved = item.AuthorizationLevel == 1 ? 1 : 0;
						var corporateApprovalHistory = new TblCorporateApprovalHistory
						{
							Id = Guid.NewGuid(),
							LogId = tranlg.Id,
							Status = nameof(AuthorizationStatus.Pending),
							ApprovalLevel = item.AuthorizationLevel,
							ApproverName = item.ApproverName,
							Description = $"Authorizer {item.AuthorizationLevel}",
							Comment = "",
							UserId = item.ApproverId,
							ToApproved = toApproved,
							CorporateCustomerId = tblCorporateCustomer.Id
						};
						tblCorporateApprovalHistories.Add(corporateApprovalHistory);
					}

					tranlg.ApprovalStatus = 0;
					tranlg.ApprovalStage = 1;
					tranlg.ApprovalCount = workflowHierarchies.Count;
					tranlg.DateProccessed = date;
					tranlg.WorkflowId = payload.WorkflowId;
					UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
					UnitOfWork.NipBulkCreditLogRepo.AddRange(bankNipBulkCreditLogList);
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.CorporateApprovalHistoryRepo.AddRange(tblCorporateApprovalHistories);
					UnitOfWork.Complete();
					var firstApproval = tblCorporateApprovalHistories.First(ctx => ctx.ApprovalLevel == 1);
					var corporateUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync(firstApproval.UserId.Value);
					var initiatorName = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)tranlg.InitiatorId);
					ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(corporateUser.Email, initiatorName.FullName, string.Format("{0:0.00}", tranlg.DebitAmount))));
				}
				return Ok(new { Responsecode = "00", ResponseDescription = "Transaction has been forwarded for approval" });
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("ApproveBulkTransfer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> ApproveBulkTransaction(ApproveTransactionDto model)
		{
			bool isSuccessful = true;
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveTransaction))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				var payload = new ApproveTransactionModel
				{
					AuthorizerId = Encryption.DecryptGuid(model.AuthorizerId),
					Comment = Encryption.DecryptStrings(model.Comment),
					Otp = Encryption.DecryptStrings(model.Otp),
					Pin = Encryption.DecryptStrings(model.Pin),
					TranLogId = Encryption.DecryptGuid(model.TranLogId),
					AuthLimitIsEnable = Encryption.DecryptInt(model.AuthLimitIsEnable)
				};

				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}
				var transactionReference = Generate16DigitNumber.Create16DigitString();
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (corporateCustomer == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				var userName = $"{CorporateProfile.Username}";
				var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", $"2FA API ERROR:{validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest(validOTP.ResponseMessage);
				}

				if (payload.AuthLimitIsEnable > 0)
				{
					var transPin = CorporateProfile.TranPin != null ? Encryption.DecriptPin(CorporateProfile.TranPin) : null;
					if (transPin == null)
					{
						return BadRequest("transaction can not be completed because transaction pin is not set");
					}
					else
					{
						if (transPin != payload.Pin)
						{
							LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", $"Invalid Pin", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
							return BadRequest("Invalid Transaction pin");
						}
					}
				}

				var pendingTranLog = UnitOfWork.NipBulkTransferLogRepo.GetByIdAsync(payload.TranLogId);
				if (pendingTranLog == null)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Invalid transaction log id", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Invalid transaction log id");
				}

				if (pendingTranLog.Status != 0)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Transaction is no longer pending approval", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Transaction is no longer pending approval");
				}

				var corporateApprovalHistory = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryByAuthId(CorporateProfile.Id, pendingTranLog.Id);
				if (corporateApprovalHistory == null)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Corporate approval history could not be retrieved", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Corporate approval history could not be retrieved");
				}
				if (corporateApprovalHistory.Status == nameof(AuthorizationStatus.Approved))
				{
					LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "This transaction has already been approved by you.", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("This transaction has already been approved by you.");
				}


				var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
				var parralexBank = _config.GetValue<string>("ParralexBank");

				var date = DateTime.Now;
				if (DailyLimitExceeded(corporateCustomer, (decimal)pendingTranLog.DebitAmount, out string errorMessage))
				{
					LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", errorMessage, JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}
				if (pendingTranLog.ApprovalCount == pendingTranLog.ApprovalStage)
				{
					var tranDate = DateTime.Now;
					var bulkSuspenseVatFee = new List<TblNipbulkCreditLog>();
					var tblPendingCreditLog = UnitOfWork.NipBulkCreditLogRepo.GetbulkCreditLog(pendingTranLog.Id);
					if (tblPendingCreditLog == null)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Credit log info could not be retrieved", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Credit log info could not be retrieved");
					}

					if (pendingTranLog.TransferType != nameof(TransactionType.Bulk_Transfer))
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Bulk Transaction only is allow", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Bulk Transaction only is allow");
					}

					var senderInfo = await _apiService.CustomerNameInquiry(pendingTranLog.DebitAccountNumber);
					if (senderInfo.ResponseCode != "00")
					{
						var message = $"Source account number could not be verified -> {senderInfo.ResponseDescription}";
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest(message);
					}
					if (senderInfo.AccountStatus != "A")
					{
						var message = $"Source account is not active transaction cannot be completed ";
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", message, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest(message);
					}
					if (senderInfo.FreezeCode != "N")
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Source account number is on debit Freeze, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Source account number is on debit Freeze, transaction can not be completed");
					}

					decimal? checkTotalDebitAmount;
					if (pendingTranLog.TotalFee != 0 && pendingTranLog.TotalVat != 0)
					{
						checkTotalDebitAmount = pendingTranLog.DebitAmount + pendingTranLog.TotalFee + pendingTranLog.TotalVat;
					}
					else
					{
						checkTotalDebitAmount = pendingTranLog.DebitAmount;
					}

					if (senderInfo.Effectiveavail < checkTotalDebitAmount)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Insufficient funds", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Insufficient funds");
					}

					var totalAmountDebit = tblPendingCreditLog.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
					if (totalAmountDebit != pendingTranLog.DebitAmount)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Transaction amount is invalid", JsonConvert.SerializeObject(totalAmountDebit), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Transaction amount is invalid");

					}
					if (totalAmountDebit == 0)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Transaction amount cannot be zero", JsonConvert.SerializeObject(totalAmountDebit), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Transaction amount cannot be zero");
					}


					var bulkSuspenseCreditItems = new List<TblNipbulkCreditLog>();
					var interBankCreditItems = tblPendingCreditLog.Where(ctx => ctx.CreditBankCode != parallexBankCode).ToList();
					var intraBankCreditItems = tblPendingCreditLog.Where(ctx => ctx.CreditBankCode == parallexBankCode).ToList();

					if (interBankCreditItems.Count != 0)
					{
						bulkSuspenseCreditItems.Add(new TblNipbulkCreditLog
						{
							Id = Guid.NewGuid(),
							TranLogId = pendingTranLog.Id,
							CreditAccountNumber = pendingTranLog.IntreBankSuspenseAccountNumber,
							CreditAccountName = pendingTranLog.IntreBankSuspenseAccountName,
							CreditAmount = Convert.ToDecimal(pendingTranLog.InterBankTotalAmount),
							CreditBankCode = parallexBankCode,
							CreditBankName = parralexBank,
							Narration = pendingTranLog.Narration,
							CreditStatus = 2,
							BatchId = pendingTranLog.BatchId,
							NameEnquiryStatus = 0,
							TryCount = 0,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							CreditDate = DateTime.Now,
						});
						bulkSuspenseVatFee.Add(new TblNipbulkCreditLog
						{
							Id = Guid.NewGuid(),
							TranLogId = pendingTranLog.Id,
							CreditAccountNumber = pendingTranLog.IntreBankSuspenseAccountNumber,
							CreditAccountName = pendingTranLog.IntreBankSuspenseAccountName,
							CreditAmount = Convert.ToDecimal(pendingTranLog.TotalVat),
							CreditBankCode = parallexBankCode,
							CreditBankName = parralexBank,
							Narration = $"VCHG|{pendingTranLog.Narration}",
							CreditStatus = 2,
							BatchId = pendingTranLog.BatchId,
							NameEnquiryStatus = 0,
							TransactionReference = Generate16DigitNumber.Create16DigitString(),
							TryCount = 0,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							CreditDate = DateTime.Now,
						});
						bulkSuspenseVatFee.Add(new TblNipbulkCreditLog
						{
							Id = Guid.NewGuid(),
							TranLogId = pendingTranLog.Id,
							CreditAccountNumber = pendingTranLog.IntreBankSuspenseAccountNumber,
							CreditAccountName = pendingTranLog.IntreBankSuspenseAccountName,
							CreditAmount = Convert.ToDecimal(pendingTranLog.TotalFee),
							CreditBankCode = parallexBankCode,
							CreditBankName = parralexBank,
							Narration = $"BCHG|{pendingTranLog.Narration}",
							CreditStatus = 2,
							BatchId = pendingTranLog.BatchId,
							TransactionReference = Generate16DigitNumber.Create16DigitString(),
							NameEnquiryStatus = 0,
							TryCount = 0,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							CreditDate = DateTime.Now,
						});
					}
					if (intraBankCreditItems.Count != 0)
					{
						bulkSuspenseCreditItems.Add(new TblNipbulkCreditLog
						{
							Id = Guid.NewGuid(),
							TranLogId = pendingTranLog.Id,
							CreditAccountNumber = pendingTranLog.SuspenseAccountNumber,
							CreditAccountName = pendingTranLog.SuspenseAccountName,
							CreditAmount = Convert.ToDecimal(pendingTranLog.IntraBankTotalAmount),
							CreditBankCode = parallexBankCode,
							CreditBankName = parralexBank,
							Narration = pendingTranLog.Narration,
							CreditStatus = 2,
							BatchId = pendingTranLog.BatchId,
							NameEnquiryStatus = 0,
							TryCount = 0,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							CreditDate = DateTime.Now,
						});
					}

					if (bulkSuspenseCreditItems.Count > 2)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "The transaction is not balanced.", JsonConvert.SerializeObject(bulkSuspenseCreditItems), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("The transaction is not balanced.");
					}

					var postBulkIntraBank = FormatBulkTransaction(bulkSuspenseCreditItems, pendingTranLog.DebitAmount, pendingTranLog);
					var postBulkIntraBankBulk = await _apiService.IntraBankBulkTransfer(postBulkIntraBank);

					if (postBulkIntraBankBulk.ResponseCode != "00")
					{

						_logger.LogError("BULK TRANSACTION ERROR {0}, {1}, {2}", Formater.JsonType(postBulkIntraBankBulk.ResponseMessage), Formater.JsonType(postBulkIntraBankBulk.ResponseCode), Formater.JsonType(postBulkIntraBankBulk.ErrorDetail));
						return BadRequest($"Transaction can not be completed at the moment -> {postBulkIntraBankBulk.ResponseMessage}");
						//if (interBankCreditItems.Count != 0)
						//{
						//	UnitOfWork.TransactionRepo.Add(new TblTransaction
						//	{
						//		Id = Guid.NewGuid(),
						//		TranAmout = pendingTranLog.InterBankTotalAmount,
						//		DestinationAcctName = pendingTranLog.IntreBankSuspenseAccountName,
						//		DestinationAcctNo = pendingTranLog.IntreBankSuspenseAccountNumber,
						//		DesctionationBank = parralexBank,
						//		TranType = "bulk",
						//		TransactionStatus = nameof(TransactionStatus.Failed),
						//		Narration = pendingTranLog.Narration,
						//		SourceAccountName = pendingTranLog.DebitAccountName,
						//		SourceAccountNo = pendingTranLog.DebitAccountNumber,
						//		SourceBank = parralexBank,
						//		CustAuthId = CorporateProfile.Id,
						//		Channel = "WEB",
						//		TransactionReference = pendingTranLog.TransactionReference,
						//		SessionId = postBulkIntraBankBulk.TrnId,
						//		TranDate = DateTime.Now,
						//		CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						//		BatchId = pendingTranLog.BatchId
						//	});
						//}
						//if (intraBankCreditItems.Count != 0)
						//{
						//	UnitOfWork.TransactionRepo.Add(new TblTransaction
						//	{
						//		Id = Guid.NewGuid(),
						//		TranAmout = pendingTranLog.IntraBankTotalAmount,
						//		DestinationAcctName = pendingTranLog.SuspenseAccountName,
						//		DestinationAcctNo = pendingTranLog.SuspenseAccountNumber,
						//		DesctionationBank = parralexBank,
						//		TranType = "bulk",
						//		TransactionStatus = nameof(TransactionStatus.Failed),
						//		Narration = pendingTranLog.Narration,
						//		SourceAccountName = pendingTranLog.DebitAccountName,
						//		SourceAccountNo = pendingTranLog.DebitAccountNumber,
						//		SourceBank = parralexBank,
						//		CustAuthId = CorporateProfile.Id,
						//		Channel = "WEB",
						//		TransactionReference = pendingTranLog.TransactionReference,
						//		SessionId = postBulkIntraBankBulk.TrnId,
						//		TranDate = DateTime.Now,
						//		CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						//		BatchId = pendingTranLog.BatchId
						//	});
						//}
						//var bulkTranLogAuditTrail = new TblAuditTrail
						//{
						//	Id = Guid.NewGuid(),
						//	ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
						//	Ipaddress = payload.IPAddress,
						//	Macaddress = payload.MACAddress,
						//	HostName = payload.HostName,
						//	ClientStaffIpaddress = payload.ClientStaffIPAddress,
						//	NewFieldValue = "Corporate User: " + CorporateProfile.FullName + " Approved Initiated transfer of " + pendingTranLog.DebitAmount + " from " + pendingTranLog.DebitAccountNumber,
						//	PreviousFieldValue = "",
						//	TransactionId = pendingTranLog.TransactionReference,
						//	UserId = CorporateProfile.Id,
						//	Username = CorporateProfile.Username,
						//	Description = "Corporate User Initiated Bulk transfer Failed, this is process with bulk transaction API",
						//	TimeStamp = DateTime.Now
						//};
						//foreach (var creditItem in tblPendingCreditLog)
						//{
						//	creditItem.CreditDate = DateTime.Now;
						//	creditItem.CreditStatus = 2;
						//	UnitOfWork.NipBulkCreditLogRepo.UpdateBulkCreditLog(creditItem);
						//}

						//pendingTranLog.ResponseCode = postBulkIntraBankBulk.ResponseCode;
						//pendingTranLog.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
						//pendingTranLog.ErrorDetail = Formater.JsonType(postBulkIntraBankBulk.ErrorDetail);
						//pendingTranLog.Status = 2;
						//pendingTranLog.TransactionStatus = 2;
						//pendingTranLog.ApprovalStatus = 1;
						//pendingTranLog.SessionId = postBulkIntraBankBulk.TrnId;
						//UnitOfWork.AuditTrialRepo.Add(bulkTranLogAuditTrail);
						//UnitOfWork.NipBulkTransferLogRepo.UpdatebulkTransfer(pendingTranLog);
						//UnitOfWork.Complete();
						//return BadRequest($"Transaction can not be completed at the moment -> {postBulkIntraBankBulk.ResponseMessage}");
					}
					if (interBankCreditItems.Count != 0)
					{
						var postVatResult = await FormatBulkTransactionCharges(bulkSuspenseVatFee, pendingTranLog);
						var checkFeeTransaction = postVatResult.Where(ctx => ctx.ResponseCode != "00").ToList();
						if (checkFeeTransaction.Count == 0)
						{
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = pendingTranLog.InterBankTotalAmount,
								DestinationAcctName = pendingTranLog.IntreBankSuspenseAccountName,
								DestinationAcctNo = pendingTranLog.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Successful),
								Narration = $"{pendingTranLog.Narration}|inter",
								SourceAccountName = pendingTranLog.DebitAccountName,
								SourceAccountNo = pendingTranLog.DebitAccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = pendingTranLog.TransactionReference,
								SessionId = postBulkIntraBankBulk.TrnId,
								TranDate = DateTime.Now,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = pendingTranLog.BatchId
							});
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = pendingTranLog.TotalVat,
								DestinationAcctName = pendingTranLog.IntreBankSuspenseAccountName,
								DestinationAcctNo = pendingTranLog.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Successful),
								Narration = $"VCHG|{pendingTranLog.Narration}",
								SourceAccountName = pendingTranLog.DebitAccountName,
								SourceAccountNo = pendingTranLog.DebitAccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = bulkSuspenseVatFee[0].TransactionReference,
								SessionId = postVatResult[0].TransactionReference,
								TranDate = DateTime.Now,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = pendingTranLog.BatchId
							});
							UnitOfWork.TransactionRepo.Add(new TblTransaction
							{
								Id = Guid.NewGuid(),
								TranAmout = pendingTranLog.TotalFee,
								DestinationAcctName = pendingTranLog.IntreBankSuspenseAccountName,
								DestinationAcctNo = pendingTranLog.IntreBankSuspenseAccountNumber,
								DesctionationBank = parralexBank,
								TranType = "bulk",
								TransactionStatus = nameof(TransactionStatus.Successful),
								Narration = $"BCHG|{pendingTranLog.Narration}",
								SourceAccountName = pendingTranLog.DebitAccountName,
								SourceAccountNo = pendingTranLog.DebitAccountNumber,
								SourceBank = parralexBank,
								CustAuthId = CorporateProfile.Id,
								Channel = "WEB",
								TransactionReference = bulkSuspenseVatFee[1].TransactionReference,
								SessionId = postVatResult[1].TransactionReference,
								TranDate = DateTime.Now,
								CorporateCustomerId = CorporateProfile.CorporateCustomerId,
								BatchId = pendingTranLog.BatchId
							});
							UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail
							{
								Id = Guid.NewGuid(),
								ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
								Ipaddress = payload.IPAddress,
								Macaddress = payload.MACAddress,
								HostName = payload.HostName,
								ClientStaffIpaddress = payload.ClientStaffIPAddress,
								NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Vat Charges of " + pendingTranLog.TotalVat + "from " + pendingTranLog.DebitAccountNumber,
								PreviousFieldValue = "",
								TransactionId = postVatResult[0].TransactionReference,
								UserId = CorporateProfile.Id,
								Username = CorporateProfile.Username,
								Description = "Corporate Bulk transfer Vat Charges",
								TimeStamp = DateTime.Now
							});
							UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail
							{
								Id = Guid.NewGuid(),
								ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
								Ipaddress = payload.IPAddress,
								Macaddress = payload.MACAddress,
								HostName = payload.HostName,
								ClientStaffIpaddress = payload.ClientStaffIPAddress,
								NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Nip Charges of " + pendingTranLog.TotalFee + "from " + pendingTranLog.DebitAccountNumber,
								PreviousFieldValue = "",
								TransactionId = postVatResult[1].TransactionReference,
								UserId = CorporateProfile.Id,
								Username = CorporateProfile.Username,
								Description = "Corporate Bulk transfer Nip Charges",
								TimeStamp = DateTime.Now
							});
						}
						else
						{
							LogFormater<BulkTransactionController>.Error(_logger, "ApproveBulkTransfer", "Bulk Inter bank fees and vats could not be processed", JsonConvert.SerializeObject(checkFeeTransaction), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						}
					}
					if (intraBankCreditItems.Count != 0)
					{
						UnitOfWork.TransactionRepo.Add(new TblTransaction
						{
							Id = Guid.NewGuid(),
							TranAmout = pendingTranLog.IntraBankTotalAmount,
							DestinationAcctName = pendingTranLog.SuspenseAccountName,
							DestinationAcctNo = pendingTranLog.SuspenseAccountNumber,
							DesctionationBank = parralexBank,
							TranType = "bulk",
							TransactionStatus = nameof(TransactionStatus.Successful),
							Narration = $"{pendingTranLog.Narration}|intra",
							SourceAccountName = pendingTranLog.DebitAccountName,
							SourceAccountNo = pendingTranLog.DebitAccountNumber,
							SourceBank = parralexBank,
							CustAuthId = CorporateProfile.Id,
							Channel = "WEB",
							TransactionReference = pendingTranLog.TransactionReference,
							SessionId = postBulkIntraBankBulk.TrnId,
							TranDate = DateTime.Now,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							BatchId = pendingTranLog.BatchId
						});
					}
					var auditTraill = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Corporate User : " + CorporateProfile.FullName + "Approved Initiated transfer of " + pendingTranLog.DebitAmount + "from " + pendingTranLog.DebitAccountNumber,
						PreviousFieldValue = "",
						TransactionId = pendingTranLog.TransactionReference,
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Corporate User Initiated Bulk transfer,  this is process with bulk transaction API",
						TimeStamp = DateTime.Now
					};
					pendingTranLog.ResponseCode = postBulkIntraBankBulk.ResponseCode;
					pendingTranLog.ResponseDescription = Formater.JsonType(postBulkIntraBankBulk.ResponseMessage);
					pendingTranLog.Status = 1;
					pendingTranLog.DateProccessed = date;
					pendingTranLog.ApprovalStatus = 1;
					pendingTranLog.ApprovalCount = 1;
					pendingTranLog.ApprovalStage = 1;
					pendingTranLog.TransactionStatus = 0;
					pendingTranLog.SessionId = postBulkIntraBankBulk.TrnId;
					UnitOfWork.NipBulkTransferLogRepo.UpdatebulkTransfer(pendingTranLog);
					UnitOfWork.AuditTrialRepo.Add(auditTraill);
					UnitOfWork.Complete();
					return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful" });

				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Approve transfer of " + pendingTranLog.DebitAmount + " from " + pendingTranLog.SuspenseAccountNumber,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Corporate User Initiated Bulk transfer",
					TimeStamp = DateTime.Now
				};
				pendingTranLog.ApprovalStage += 1;
				corporateApprovalHistory.Status = nameof(AuthorizationStatus.Approved);
				corporateApprovalHistory.ToApproved = 0;
				corporateApprovalHistory.ApprovalDate = date;
				corporateApprovalHistory.Comment = payload.Comment;
				corporateApprovalHistory.UserId = CorporateProfile.Id;
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.NipBulkTransferLogRepo.UpdatebulkTransfer(pendingTranLog);
				UnitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(corporateApprovalHistory);
				UnitOfWork.Complete();

				var newTransApprover = UnitOfWork.CorporateApprovalHistoryRepo.GetNextBulkApproval(pendingTranLog);
				if (newTransApprover != null)
				{
					newTransApprover.ToApproved = 1;
					UnitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(newTransApprover);
					UnitOfWork.Complete();
					var approvalInfo = UnitOfWork.CorporateProfileRepo.GetByIdAsync(CorporateProfile.Id);
					var initiatorInfo = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog?.InitiatorId);
					var dto = new EmailNotification
					{
						Action = nameof(AuthorizationStatus.Approved),
						Amount = $"{pendingTranLog.DebitAmount:0.00}"
					};
					_notify.NotifyCorporateTransfer(initiatorInfo, approvalInfo, dto, payload.Comment);
				}
				return Ok(isSuccessful);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("DeclineTransaction")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> DeclineTransaction(DeclineTransactionDto model)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}
				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveTransaction))
				{
					return BadRequest("UnAuthorized Access");
				}
				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				var payload = new DeclineTransactionModel
				{
					Comment = Encryption.DecryptStrings(model.Comment),
					Otp = Encryption.DecryptStrings(model.Otp),
					TranLogId = Encryption.DecryptGuid(model.TranLogId),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
				};

				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (corporateCustomer == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveTransaction))
				{
					return BadRequest("UnAuthorized Access");
				}

				var userName = $"{CorporateProfile.Username}";
				var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<BulkTransactionController>.Error(_logger, "DeclineTransaction", $"2FA API ERROR:{validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest(validOTP.ResponseMessage);
				}

				var pendingTranLog = UnitOfWork.NipBulkTransferLogRepo.GetByIdAsync(payload.TranLogId);
				if (pendingTranLog == null)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "DeclineTransaction", "Invalid transaction log id", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Invalid transaction log id");
				}

				if (pendingTranLog.Status != (int)ProfileStatus.Pending)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "DeclineTransaction", "Transaction is no longer pending approval", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Transaction is no longer pending approval");
				}
				var tblPendingCreditLog = UnitOfWork.NipBulkCreditLogRepo.GetbulkCreditLog(pendingTranLog.Id);
				if (tblPendingCreditLog == null)
				{
					LogFormater<BulkTransactionController>.Error(_logger, "DeclineTransaction", "Credit log info could not be retrieved", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Credit log info could not be retrieved");
				}

				var creditLogList = new List<TblNipbulkCreditLog>();
				foreach (var item in tblPendingCreditLog)
				{
					item.CreditStatus = (int)ProfileStatus.Declined;
					item.CreditDate = DateTime.Now;
					creditLogList.Add(item);
				}
				var parallexSuspenseAccount = _config.GetValue<string>("NIPSBulkSuspenseAccount");
				var corporateApprovalHistory = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryByAuthId(CorporateProfile.Id, pendingTranLog.Id);
				pendingTranLog.Status = (int)ProfileStatus.Declined;
				pendingTranLog.TransactionStatus = (int)ProfileStatus.Declined;
				pendingTranLog.ApprovalStatus = 1;
				corporateApprovalHistory.Status = nameof(AuthorizationStatus.Decline);
				corporateApprovalHistory.ToApproved = 0;
				corporateApprovalHistory.ApprovalDate = DateTime.Now;
				corporateApprovalHistory.Comment = payload.Comment;

				// add to auditri
				var failedauditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + " Decline transfer of " + pendingTranLog.DebitAmount + " from " + pendingTranLog.DebitAccountNumber + " To Suspense Account  " + parallexSuspenseAccount,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = $"Corporate Authorizer Decline Bulk transfer reason been: {payload.Comment}",
					TimeStamp = DateTime.Now
				};
				//update tables
				UnitOfWork.AuditTrialRepo.Add(failedauditTrail);
				UnitOfWork.NipBulkTransferLogRepo.UpdatebulkTransfer(pendingTranLog);
				UnitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(corporateApprovalHistory);
				UnitOfWork.NipBulkCreditLogRepo.UpdateBulkCreditLogList(creditLogList);
				UnitOfWork.Complete();

				var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog?.InitiatorId);
				var dto = new EmailNotification
				{
					Action = nameof(AuthorizationStatus.Decline),
					Amount = string.Format("{0:0.00}", pendingTranLog.DebitAmount)
				};
				_notify.NotifyCorporateTransfer(initiatorProfile, null, dto, payload.Comment);
				return Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("PendingBulkTransactionLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> PendingBulkTransactionLogs()
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (tblCorporateCustomer == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewPendingTransaction))
				{
					if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
					{
						if (_authType != AuthorizationType.Single_Signatory)
						{
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<BulkTransactionController>.Error(_logger, "PendingBulkTransactionLogs", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}
				var tblTransactions = UnitOfWork.NipBulkTransferLogRepo.GetBulkPendingTransferLog(tblCorporateCustomer.Id);
				if (!tblTransactions.Any())
				{
					return Ok(new ListResponseDTO<ApproveBulkTransactionResponse>(_data: null, success: true, _message: Message.Success));
				}
				var transactionHistory = new List<ApproveBulkTransactionResponse>();
				foreach (var item in tblTransactions)
				{
					var response = new ApproveBulkTransactionResponse
					{
						AuthLimit = tblCorporateCustomer.AuthenticationLimit != null ? 1 : 0,
						AuthLimitIsEnable = item.DebitAmount >= tblCorporateCustomer.AuthenticationLimit ? 1 : 0,
						PendingTransaction = item,
						Sn = item.Sn,
					};
					transactionHistory.Add(response);
				}
				var result = transactionHistory.OrderByDescending(x => x.Sn).ToList();
				return Ok(new ListResponseDTO<ApproveBulkTransactionResponse>(_data: result, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("AuthorizedBulkTransactionLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> AuthorizedBulkTransactionLogs(string corporateCustomerId)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}

				if (string.IsNullOrEmpty(corporateCustomerId))
				{
					return BadRequest("Corporate Customer Id is required");
				}

				var Id = Encryption.DecryptGuid(corporateCustomerId);
				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(Id);
				if (tblCorporateCustomer == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewTransactionHistory))
				{
					if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
					{
						if (_authType != AuthorizationType.Single_Signatory)
						{
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<BulkTransactionController>.Error(_logger, "AuthorizedBulkTransactionLogs", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var tblTransactions = UnitOfWork.NipBulkTransferLogRepo.GetAuthorizedBulkTransactions(tblCorporateCustomer.Id);

				if (tblTransactions != null && tblTransactions?.Count > 0)
				{
					return Ok(tblTransactions.OrderByDescending(x => x.Sn));
				}
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("DeclineTransactions")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblPendingTranLog>> DeclineTransactionLogs()
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}
				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewPendingTransaction))
				{
					if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
					{
						if (_authType != AuthorizationType.Single_Signatory)
						{
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<BulkTransactionController>.Error(_logger, "DeclineTransactions", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var pendingTranLogs = UnitOfWork.NipBulkTransferLogRepo.GetAllDeclineTransaction((Guid)CorporateProfile.CorporateCustomerId).ToList();

				if (pendingTranLogs?.Count > 0)
				{
					return Ok(pendingTranLogs.OrderByDescending(x => x.Sn));
				}

				return Ok(pendingTranLogs);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("BulkCreditLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> BulkCreditLogs(string bulkFileId)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}

				if (string.IsNullOrEmpty(bulkFileId))
				{
					return BadRequest("Bulk file Id is required");
				}

				var id = Encryption.DecryptGuid(bulkFileId);
				var tblTransactions = UnitOfWork.NipBulkCreditLogRepo.GetbulkCreditLog(id);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("FailedBulkCreditLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> FailedBulkCreditLogs(string bulkFileId)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}

				if (string.IsNullOrEmpty(bulkFileId))
				{
					return BadRequest("Bulk file Id is required");
				}

				var id = Encryption.DecryptGuid(bulkFileId);
				var tblTransactions = UnitOfWork.NipBulkCreditLogRepo.GetbulkCreditLogStatus(id, 2);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("BulkTransactionApprovalHistory")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblCorporateBulkApprovalHistory>> BulkTransactionApprovalHistory(string bulkFileId)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errorMsg))
				{
					return StatusCode(400, errorMsg);
				}

				if (string.IsNullOrEmpty(bulkFileId))
				{
					return BadRequest("Bulk file Id is required");
				}

				var id = Encryption.DecryptGuid(bulkFileId);
				var tblTransactions = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateBulkAuthorizationHistories(id);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("DownloadTemplate")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> GetInterbankUploadSample()
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}
				//path to file
				var filePath = Path.Combine("wwwroot", "bulkupload", "sample.xlsx");
				if (!System.IO.File.Exists(filePath))
					return NotFound();
				var memory = new MemoryStream();
				await using (var stream = new FileStream(filePath, FileMode.Open))
				{
					await stream.CopyToAsync(memory);
				}
				memory.Position = 0;
				return File(memory, Formater.GetContentType(filePath), "bulk upload template.xlsx");
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("DownloadBankCodeTemplate")]
		public async Task<IActionResult> DownloadBankCodeTemplate()
		{
			if (!IsAuthenticated)
			{
				return StatusCode(401, "User is not authenticated");
			}
			//path to file
			//var folderName = Path.Combine("wwwroot", "bulkupload");
			var filePath = Path.Combine("wwwroot", "BulkUpload", "bankcodetemplate.xlsx");
			if (!System.IO.File.Exists(filePath))
				return NotFound();
			var memory = new MemoryStream();
			await using (var stream = new FileStream(filePath, FileMode.Open))
			{
				await stream.CopyToAsync(memory);
			}
			memory.Position = 0;
			return File(memory, Formater.GetContentType(filePath), "bank code bankcodetemplate.xlsx");
		}

		private ValidationStatus ValidateWorkflowAccess(Guid? workflowId, decimal amount)
		{
			if (workflowId != null)
			{
				var workFlow = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)workflowId);
				if (workFlow == null)
				{
					return new ValidationStatus { Status = false, Message = "Workflow is invalid" };
				}

				if (workFlow.Status != 1)
				{
					return new ValidationStatus { Status = false, Message = "Workflow selected is not active" };
				}
				var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(workFlow.Id);
				if (workflowHierarchies.Count == 0)
				{
					return new ValidationStatus { Status = false, Message = "No Workflow Hierarchies found" };
				}
				if (workflowHierarchies.Count != workFlow.NoOfAuthorizers)
				{
					return new ValidationStatus { Status = false, Message = "Workflow Authorize is not valid " };
				}

			}
			return new ValidationStatus { Status = true, Message = "Validation OK" };
		}
		private bool DailyLimitExceeded(TblCorporateCustomer tblCorporateCustomer, decimal amount, out string errorMsg)
		{
			errorMsg = string.Empty;
			var customerDailyTransLimitHistory = _unitOfWork.TransactionHistoryRepo.GetTransactionHistory(tblCorporateCustomer.Id, DateTime.Now.Date);
			if (customerDailyTransLimitHistory != null)
			{
				if (tblCorporateCustomer.BulkTransDailyLimit != null)
				{
					if (customerDailyTransLimitHistory.BulkTransTotalAmount != null)
					{
						decimal amtTransferable = (decimal)tblCorporateCustomer.BulkTransDailyLimit - (decimal)customerDailyTransLimitHistory.BulkTransTotalAmount;

						if (amtTransferable < amount)
						{
							if (amtTransferable <= 0)
							{
								errorMsg = $"You have exceeded your daily bulk transaction limit Which is {Helper.formatCurrency(tblCorporateCustomer.BulkTransDailyLimit)}";
								return true;
							}
							errorMsg = $"Transaction amount {Helper.formatCurrency(amount)} has exceeded the maximum daily transaction limit {Helper.formatCurrency(tblCorporateCustomer.BulkTransDailyLimit)} for your organisation. You can only transfer {Helper.formatCurrency(amtTransferable)} for the rest of the day";
							return true;
						}
					}
				}
			}
			return false;
		}
		private static BulkIntrabankTransactionModel FormatBulkTransaction(List<TblNipbulkCreditLog> bulkTransaction, decimal? totalDebitAmount, TblNipbulkTransferLog creditLog)
		{
			var narrationTuple = creditLog.Narration.Length > 50 ? Tuple.Create(creditLog.Narration[..50], creditLog.Narration[50..]) : Tuple.Create(creditLog.Narration, "");
			var trnParticular2 = narrationTuple.Item2.Length > 50 ? narrationTuple.Item2[..50] : narrationTuple.Item2;
			var tranDate = DateTime.Now;
			var creditItems = new List<PartTrnRec>();
			var beneficiary = new PartTrnRec
			{
				AcctId = creditLog.DebitAccountNumber,
				CreditDebitFlg = "D",
				TrnAmt = totalDebitAmount.ToString(),
				currencyCode = "NGN",
				TrnParticulars = narrationTuple.Item1,
				ValueDt = tranDate.ToString("MM/dd/yyyy HH:mm:ss"),
				PartTrnRmks = creditLog.TransactionReference,
				REFNUM = "",
				RPTCODE = "",
				TRANPARTICULARS2 = trnParticular2
			};
			creditItems.Add(beneficiary);
			foreach (var item in bulkTransaction)
			{
				var tranNarration = item.Narration.Length > 50 ? Tuple.Create(item.Narration[..50], item.Narration[50..]) : Tuple.Create(item.Narration, "");
				var trnParticulars2 = tranNarration.Item2.Length > 50 ? tranNarration.Item2[..50] : tranNarration.Item2;
				var creditBeneficiary = new PartTrnRec
				{
					AcctId = item.CreditAccountNumber,
					CreditDebitFlg = "C",
					TrnAmt = item.CreditAmount.ToString(),
					currencyCode = "NGN",
					TrnParticulars = tranNarration.Item1,
					ValueDt = tranDate.ToString("MM/dd/yyyy HH:mm:ss"),
					PartTrnRmks = Generate16DigitNumber.Create16DigitString(),
					REFNUM = "",
					RPTCODE = "",
					TRANPARTICULARS2 = trnParticulars2
				};
				creditItems.Add(creditBeneficiary);
			};
			var intraBankBulkTransfer = new BulkIntrabankTransactionModel
			{
				BankId = "01",
				TrnType = "T",
				TrnSubType = "CI",
				RequestID = Generate16DigitNumber.Create16DigitString(),
				PartTrnRec = creditItems,
			};
			return intraBankBulkTransfer;
		}
		private async Task<List<IntraBankTransferResponse>> FormatBulkTransactionCharges(List<TblNipbulkCreditLog> bulkTransaction, TblNipbulkTransferLog creditLog)
		{
			var responseResult = new List<IntraBankTransferResponse>();
			foreach (var item in bulkTransaction)
			{
				var narrationTuple = item.Narration.Length > 50 ? Tuple.Create(item.Narration[..50], item.Narration[50..]) : Tuple.Create(item.Narration, "");
				var date = DateTime.Now;
				var transfer = new IntraBankPostDto
				{
					AccountToDebit = creditLog.DebitAccountNumber,
					UserName = CorporateProfile.Username,
					Channel = "2",
					TransactionLocation = creditLog.TransactionLocation,
					IntraTransferDetails = new List<IntraTransferDetail>{
							new IntraTransferDetail {
								TransactionReference = item.TransactionReference,
								TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
								BeneficiaryAccountName = creditLog.IntreBankSuspenseAccountName,
								BeneficiaryAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
								Amount = item.CreditAmount,
								Narration = narrationTuple.Item1
							}
						}
				};
				var transferResult = await _apiService.IntraBankTransfer(transfer);
				if (transferResult.ResponseCode != "00")
				{
					//transferResult.HasFailed = true;
					responseResult.Add(transferResult);
				}
				else
				{
					//transferResult.HasFailed = false;
					responseResult.Add(transferResult);
				}
			}
			return responseResult;
		}

		private async Task<GenericStatus> ValidateRelatedAccount(TblCorporateCustomer? corporateCustomer, string accountNumber)
		{
			var validateStatus = new GenericStatus();
			if (corporateCustomer?.DefaultAccountNumber.Trim() != accountNumber.Trim())
			{
				var isAggregationEnable = UnitOfWork.CorporateAggregationRepo.GetCorporateCustomerAggregations(corporateCustomer?.Id);
				if (isAggregationEnable.Any())
				{
					var validateAccount = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAccountNumberAndCorporateCustomer(accountNumber, corporateCustomer.Id);
					if (validateAccount == null)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "BulkTransaction ValidateRelatedAccount", accountNumber, JsonConvert.SerializeObject(validateAccount));
						validateStatus.Message = "Source Account Verification Failed";
						validateStatus.Status = false;
						return validateStatus;
					}

					var getRelatedAccountCustomerId = isAggregationEnable.FirstOrDefault(ctx => ctx.Id == validateAccount.AccountAggregationId);
					if (getRelatedAccountCustomerId == null)
					{
						LogFormater<BulkTransactionController>.Error(_logger, "BulkTransaction ValidateRelatedAccount", accountNumber, JsonConvert.SerializeObject(validateAccount));
						validateStatus.Message = "The Selected Account cannot be verified";
						validateStatus.Status = false;
						return validateStatus;
					}
					var result = await _apiService.RelatedCustomerAccountDetails(getRelatedAccountCustomerId.CustomerId);
					if (!AccountValidation.RelatedAccount(result, accountNumber, out string validationtError))
					{
						LogFormater<BulkTransactionController>.Error(_logger, "BulkTransaction ValidateRelatedAccount", validationtError, JsonConvert.SerializeObject(getRelatedAccountCustomerId.CustomerId));
						validateStatus.Message = validationtError;
						validateStatus.Status = false;
						return validateStatus;
					}
					validateStatus.Message = validationtError;
					validateStatus.Status = true;
					return validateStatus;
				}

				var corporateAccountt = await _apiService.RelatedCustomerAccountDetails(corporateCustomer?.CustomerId);
				if (!AccountValidation.RelatedAccount(corporateAccountt, accountNumber, out string aggregateErrorMessage))
				{
					LogFormater<BulkTransactionController>.Error(_logger, "BulkTransaction ValidateRelatedAccount", aggregateErrorMessage, JsonConvert.SerializeObject(corporateCustomer?.CustomerId));
					validateStatus.Message = aggregateErrorMessage;
					validateStatus.Status = false;
					return validateStatus;
				}
			}
			var corporateAccount = await _apiService.RelatedCustomerAccountDetails(corporateCustomer?.CustomerId);
			if (!AccountValidation.RelatedAccount(corporateAccount, accountNumber, out string aggregatedErrorMessage))
			{
				LogFormater<BulkTransactionController>.Error(_logger, "BulkTransaction ValidateRelatedAccount", aggregatedErrorMessage, JsonConvert.SerializeObject(corporateCustomer?.CustomerId));
				validateStatus.Message = aggregatedErrorMessage;
				validateStatus.Status = false;
				return validateStatus;
			}

			validateStatus.Message = aggregatedErrorMessage;
			validateStatus.Status = true;
			return validateStatus;
		}

	}
}
