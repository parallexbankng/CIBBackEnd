using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Exceptions;
using CIB.Core.Modules.BulkTransaction.Dto;
using CIB.Core.Modules.Transaction._PendingCreditLog.Dto;
using CIB.Core.Modules.Transaction.Dto;
using CIB.Core.Modules.Transaction.Dto.Interbank;
using CIB.Core.Modules.Transaction.Dto.Intrabank;
using CIB.Core.Modules.Transaction.Validation;
using CIB.Core.Modules.TransactionLimitHistory.dto;
using CIB.Core.Services._2FA;
using CIB.Core.Services.Api;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Services.Notification;
using CIB.Core.Templates;
using CIB.Core.Utils;
using CIB.CorporateAdmin.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class TransactionController : BaseAPIController
	{
		private readonly IApiService _apiService;
		private readonly IEmailService _emailService;
		private readonly IFileService _fileService;
		private readonly IConfiguration _config;
		private readonly IToken2faService _2fa;
		private readonly ILogger<TransactionController> _logger;
		protected readonly INotificationService notify;

		public TransactionController(
			IAuthenticationService authService,
			INotificationService notify,
			ILogger<TransactionController> logger,
			IApiService apiService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			IHttpContextAccessor accessor,
			IEmailService emailService,
			IFileService fileService,
			IConfiguration config,
			IToken2faService Zfa) : base(unitOfWork, mapper, accessor, authService)
		{
			_apiService = apiService;
			_emailService = emailService;
			_fileService = fileService;
			_config = config;
			_2fa = Zfa;
			_logger = logger;
			this.notify = notify;
		}

		[HttpGet("GetTransactionTypes")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public ActionResult<List<TransactionTypeModel>> GetTransactionTypes()
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				string errormsg = string.Empty;

				if (!IsUserActive(out errormsg))
				{
					return StatusCode(400, errormsg);
				}

				// string IP = IPAddress;

				var transactionTypes = new List<TransactionTypeModel>();
				var enums = Enum.GetValues(typeof(TransactionType)).Cast<TransactionType>().ToList();
				foreach (var e in enums)
				{
					transactionTypes.Add(new TransactionTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
				}

				return Ok(transactionTypes);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("GetCorporateAccounts")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<List<BankAccountModel>>> GetCorporateAccounts(string corporateCustomerId)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}
				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
				}
				if (string.IsNullOrEmpty(corporateCustomerId))
				{
					return BadRequest("Corporate Customer Id is required");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}
				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateAccount))
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
						LogFormater<TransactionController>.Error(_logger, "Get Corporate Accounts", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
						return BadRequest("Authorization type could not be determined!!!");
					}
				}
				var Id = Encryption.DecryptGuid(corporateCustomerId);
				if (Id != CorporateProfile.CorporateCustomerId)
				{
					LogFormater<TransactionController>.Error(_logger, "Get Corporate Accounts", "Invalid corporate customer id", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
					return BadRequest("Invalid corporate customer id");
				}

				var myAccountNumber = new RelatedCustomerAccountDetailsDto();
				var accountNumbers = new List<RelatedCustomerAccountDetail>();
				var getAggregateAccount = UnitOfWork.CorporateAggregationRepo.GetCorporateCustomerAggregations(CorporateProfile.CorporateCustomerId);
				if (getAggregateAccount.Any())
				{
					var dtoo = await _apiService.RelatedCustomerAccountDetails(tblCorporateCustomer.CustomerId);
					if (dtoo.RespondCode != "00")
					{
						LogFormater<TransactionController>.Error(_logger, "Get Corporate Accounts", dtoo.RespondMessage, JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
						return BadRequest(dtoo.RespondMessage);
					}
					myAccountNumber.RespondMessage = dtoo.RespondMessage;
					myAccountNumber.RespondCode = dtoo.RespondCode;
					myAccountNumber.Requestid = dtoo.Requestid;
					accountNumbers.AddRange(dtoo?.Records);
					var relatedAccountDetails = await GetRelatedAccountNumber(getAggregateAccount);
					if (relatedAccountDetails.Any())
					{
						accountNumbers.AddRange(relatedAccountDetails);
						myAccountNumber.Records = accountNumbers;

					}
				}
				else
				{
					var result = await _apiService.RelatedCustomerAccountDetails(tblCorporateCustomer.CustomerId);
					accountNumbers.AddRange(result.Records);
					myAccountNumber.Records = accountNumbers;
				}
				return Ok(myAccountNumber);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("VerifyIntraBankTransaction")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> VerifyIntraBankTransaction(IntraBankTransactionDto model)
		{
			try
			{
				string errormsg = string.Empty;
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

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
						return BadRequest("Authorization type could not be determined!!!");
					}
				}
				var payload = new IntraBankTransaction
				{
					Amount = Encryption.DecryptDecimals(model.Amount),
					DestinationAccountName = Encryption.DecryptStrings(model.DestinationAccountName),
					SourceAccountName = Encryption.DecryptStrings(model.SourceAccountName),
					SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
					DestinationBankCode = Encryption.DecryptStrings(model.DestinationBankCode),
					DestinationBankName = Encryption.DecryptStrings(model.DestinationBankName),
					Narration = Encryption.DecryptStrings(model.Narration),
					SourceBankName = Encryption.DecryptStrings(model.SourceBankName),
					TransactionType = Encryption.DecryptStrings(model.TransactionType),
					WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
					Otp = Encryption.DecryptStrings(model.Otp)
				};

				// var validator = new InitiaIntraBankTransactionValidation();
				// var results =  validator.Validate(payload);
				// if (!results.IsValid)
				// {
				//     return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
				// }

				try
				{
					payload.Otp = "";
					payload.DestinationAccountNumber = Encryption.DecryptDebitAccount(model.DestinationAccountNumber);
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("Padding is invalid and cannot be removed"))
					{
						payload.Otp = "";
						LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "beneficiary Account Number could not be verify", JsonConvert.SerializeObject(payload), "");
						return BadRequest("beneficiary Account Number could not be verify");
					}
					return StatusCode(500, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
				}

				if (tblCorporateCustomer.MinAccountLimit > payload.Amount)
				{
					payload.Otp = "";
					var errorMessage = $"Transcation amount {payload.Amount} is below the Minimum transaction amount  {tblCorporateCustomer.MinAccountLimit} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), "");
					return BadRequest(errorMessage);
				}

				if (tblCorporateCustomer.MaxAccountLimit < payload.Amount)
				{
					payload.Otp = "";
					var errorMessage = $"Transcation amount {payload.Amount} has exceeded the maximum transaction amount {tblCorporateCustomer.MaxAccountLimit} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), "");
					return BadRequest(errorMessage);
				}

				var validateRelatedAccount = await this.ValidateRelatedAccount(tblCorporateCustomer, payload.SourceAccountNumber);
				if (!validateRelatedAccount.Status)
				{
					return BadRequest(validateRelatedAccount.Message);
				}
				if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
				{
					if (_auth == AuthorizationType.Single_Signatory)
					{

					}
					else
					{
						if (CorporateProfile.ApprovalLimit < payload.Amount)
						{
							payload.Otp = "";
							var errorMessage = $"Transaction amount {payload.Amount} has exceeded your transaction limit {CorporateProfile.ApprovalLimit}";
							LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(errorMessage);
						}

						if (string.IsNullOrEmpty(payload.WorkflowId.ToString()))
						{
							payload.Otp = "";
							LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Workflow is required", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Workflow is required");
						}
						var validateWorkFlow = ValidateWorkflowAccess(payload.WorkflowId, payload.Amount);

						if (!validateWorkFlow.Status)
						{
							payload.Otp = "";
							LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", validateWorkFlow.Message, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(validateWorkFlow.Message);
						}
					}
				}
				else
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Authorization type could not be determined!!!");
				}

				//call name inquiry API
				var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);

				if (senderInfo.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Source account number could not be verified", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number could not be verified");
				}

				if (senderInfo.AccountStatus != "A")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Source account number is not Active, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is not Active, transaction can not be completed");
				}

				if (senderInfo.FreezeCode != "N")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Source account number is on debit Freeze, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is on debit Freeze, transaction can not be completed");
				}

				if (senderInfo.CurrencyCode != "NGN")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Only naira Transaction is allow , transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Only naira Transaction is allow , transaction can not be completed");
				}

				if (senderInfo.Effectiveavail < payload.Amount)
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Insufficient funds", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Insufficient funds");
				}

				var receiverInfo = await _apiService.CustomerNameInquiry(payload.DestinationAccountNumber);
				if (receiverInfo.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Receiver account number could not be verified", JsonConvert.SerializeObject(receiverInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Receiver account number could not be verified");
				}
				var response = new VerifyBulkTransactionResponse
				{
					AuthLimit = tblCorporateCustomer.AuthenticationLimit != null ? 1 : 0,
					AuthLimitIsEnable = payload.Amount >= tblCorporateCustomer.AuthenticationLimit ? 1 : 0,
				};
				return Ok(response);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("InitiateIntraBankTransaction")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> InitiateIntraBankTransaction(IntraBankTransactionDto model)
		{
			bool isSuccessful = true;
			try
			{
				string errormsg = string.Empty;
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				//Guid? corporateCustomerId = UserProfile.CorporateCustomerId;
				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

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
						LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var payload = new IntraBankTransaction
				{
					Amount = Encryption.DecryptDecimals(model.Amount),
					DestinationAccountName = Encryption.DecryptStrings(model.DestinationAccountName),
					SourceAccountName = Encryption.DecryptStrings(model.SourceAccountName),
					SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
					DestinationBankCode = Encryption.DecryptStrings(model.DestinationBankCode),
					DestinationBankName = Encryption.DecryptStrings(model.DestinationBankName),
					Narration = Encryption.DecryptStrings(model.Narration),
					SourceBankName = Encryption.DecryptStrings(model.SourceBankName),
					TransactionType = Encryption.DecryptStrings(model.TransactionType),
					WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
					TransactionLocation = Encryption.DecryptStrings(model.TransactionLocation),
					Otp = Encryption.DecryptStrings(model.Otp),
					Pin = Encryption.DecryptStrings(model.Pin),
					AuthLimitIsEnable = Encryption.DecryptInt(model.AuthLimitIsEnable),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.IPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var validator = new InitiaIntraBankTransactionValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Entity Validation", JsonConvert.SerializeObject(results.Errors), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}


				try
				{
					payload.DestinationAccountNumber = Encryption.DecryptDebitAccount(model.DestinationAccountNumber);
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("Padding is invalid and cannot be removed"))
					{
						payload.TransactionLocation = "";
						payload.Otp = "";
						LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "beneficiary Account Number could not be verify", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("beneficiary Account Number could not be verify");
					}
					return StatusCode(500, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
				}

				var userName = $"{CorporateProfile.Username}";
				var validOTP = await _2fa.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", $"2FA API ERROR:{validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
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
							LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", $"Invalid Pin", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Invalid Transaction pin");
						}
					}
				}

				if (tblCorporateCustomer.MaxAccountLimit < payload.Amount)
				{
					payload.TransactionLocation = "";
					payload.Otp = "";
					var errorMessage = $"Transaction amount {payload.Amount} has exceeded the maximum transaction amount {tblCorporateCustomer.MaxAccountLimit} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}

				//check daily transfer limit
				if (tblCorporateCustomer.SingleTransDailyLimit < payload.Amount)
				{
					payload.TransactionLocation = "";
					payload.Otp = "";
					var errorMessage = $"Transaction amount {Helper.formatCurrency(payload.Amount)} has exceeded the maximum daily transaction limit {Helper.formatCurrency(tblCorporateCustomer.SingleTransDailyLimit)} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}

				//check cummulatve daily transfer limit
				if (DailyLimitExceeded(tblCorporateCustomer, payload.Amount, out errormsg))
				{
					payload.TransactionLocation = ""; payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", errormsg, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errormsg);
				}
				if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
				{
					if (_auth != AuthorizationType.Single_Signatory)
					{
						if (CorporateProfile.ApprovalLimit < payload.Amount)
						{
							payload.TransactionLocation = ""; payload.Otp = "";
							var errorMessage = $"Transaction amount {payload.Amount} has exceeded your transaction limit {CorporateProfile.ApprovalLimit}";
							LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(errorMessage);
						}

						if (payload.WorkflowId == default)
						{
							payload.TransactionLocation = ""; payload.Otp = "";
							LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Workflow is required", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Workflow is required");
						}
						var validateWorkFlow = ValidateWorkflowAccess(payload.WorkflowId, payload.Amount);
						if (!validateWorkFlow.Status)
						{
							payload.TransactionLocation = ""; payload.Otp = "";
							LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", validateWorkFlow.Message, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(validateWorkFlow.Message);
						}
					}
				}
				else
				{
					payload.TransactionLocation = ""; payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Authorization type could not be determined!!!");
				}
				var validateRelatedAccount = await this.ValidateRelatedAccount(tblCorporateCustomer, payload.SourceAccountNumber);
				if (!validateRelatedAccount.Status)
				{
					return BadRequest(validateRelatedAccount.Message);
				}


				//call name inquiry API
				var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);
				if (senderInfo.ResponseCode != "00")
				{
					payload.TransactionLocation = ""; payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Source account number could not be verified", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number could not be verified");
				}
				if (senderInfo.AccountStatus != "A")
				{
					payload.TransactionLocation = ""; payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Source account number is not Active", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is not Active");
				}
				if (senderInfo.FreezeCode != "N")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Source account number is on debit Freeze, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is on debit Freeze, transaction can not be completed");
				}
				if (senderInfo.CurrencyCode != "NGN")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyIntraBankTransaction", "Only naira Transaction is allow , transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Only naira Transaction is allow , transaction can not be completed");
				}

				var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
				var receiverInfo = await _apiService.BankNameInquire(payload.DestinationAccountNumber, parallexBankCode);
				if (receiverInfo.ResponseCode != "00")
				{
					payload.TransactionLocation = ""; payload.Otp = "";
					var errorMessage = $"Receiver account number could not be verified ->{receiverInfo.ResponseMessage}";
					LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}


				var tblCorporateApprovalHistories = new List<TblCorporateApprovalHistory>();
				var date = DateTime.Now;
				var pendingTranLog = new TblPendingTranLog();
				var pendingCreditLog = new TblPendingCreditLog();
				var corporateApprovalHistoryList = new List<TblCorporateApprovalHistory>();
				var transactionReference = Generate16DigitNumber.Create16DigitString();
				var batchId = Guid.NewGuid();
				var narration = $"IP|{tblCorporateCustomer.CompanyName}|{receiverInfo.AccountName}|{payload.Narration}";
				string transactionType = payload.TransactionType?.Replace(" ", "_") == nameof(TransactionType.Own_Transfer) ? nameof(TransactionType.Own_Transfer).Replace("_", " ") : nameof(TransactionType.Intra_Bank_Transfer).Replace("_", " ");

				if (_auth == AuthorizationType.Single_Signatory)
				{
					if (senderInfo.Effectiveavail < payload.Amount)
					{
						payload.TransactionLocation = ""; payload.Otp = "";
						LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Insufficient funds", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Insufficient funds");
					}
					pendingTranLog.Id = Guid.NewGuid();
					pendingTranLog.BatchId = batchId;
					pendingTranLog.ApprovalCount = 1;
					pendingTranLog.ApprovalStage = 1;
					pendingTranLog.CompanyId = tblCorporateCustomer.Id;
					pendingTranLog.TransferType = transactionType;
					pendingTranLog.DateInitiated = date;
					pendingTranLog.Narration = narration;
					pendingTranLog.DebitAccountName = senderInfo.AccountName;
					pendingTranLog.DebitAccountNumber = senderInfo.AccountNumber;
					pendingTranLog.DebitAmount = payload.Amount;
					pendingTranLog.InitiatorId = CorporateProfile.Id;
					pendingTranLog.NoOfCredits = 1;
					pendingTranLog.OriginatorBvn = senderInfo.Bvn;
					pendingTranLog.Currency = "NGN";
					pendingTranLog.TransactionLocation = payload.TransactionLocation;


					pendingCreditLog.Id = Guid.NewGuid();
					pendingCreditLog.TranLogId = pendingTranLog.Id;
					pendingCreditLog.CreditAccountNumber = receiverInfo.AccountNumber;
					pendingCreditLog.CreditAccountName = receiverInfo.AccountName;
					pendingCreditLog.CreditAmount = payload.Amount;
					pendingCreditLog.CreditBankCode = parallexBankCode;
					pendingCreditLog.CreditBankName = "Parralex Bank";
					pendingCreditLog.BankVerificationNo = receiverInfo.BVN;
					pendingCreditLog.KycLevel = receiverInfo.KYCLevel;
					pendingCreditLog.NameEnquiryRef = receiverInfo.RequestId;
					pendingCreditLog.ResponseCode = receiverInfo.ResponseCode;
					pendingCreditLog.ResponseMessage = receiverInfo.ResponseMessage;
					pendingCreditLog.ChannelCode = "2";
					pendingCreditLog.Narration = narration;
					pendingCreditLog.CorporateCustomerId = tblCorporateCustomer.Id;
					pendingCreditLog.TryCount = 0;

					var transfer = new IntraBankPostDto
					{
						AccountToDebit = payload.SourceAccountNumber,
						UserName = CorporateProfile.Username,
						Channel = "2",
						TransactionLocation = payload.TransactionLocation,
						IntraTransferDetails = new List<IntraTransferDetail>
						{
							new IntraTransferDetail
							{
								TransactionReference = transactionReference,
								TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
								BeneficiaryAccountName = receiverInfo.AccountName,
								BeneficiaryAccountNumber = payload.DestinationAccountNumber,
								Amount = payload.Amount,
								Narration = narration
							}
						}
					};
					var transferResult = await _apiService.IntraBankTransfer(transfer);
					if (transferResult.ResponseCode != "00")
					{
						LogFormater<TransactionController>.Error(_logger, "InitiateIntraBankTransaction", "Failed Transaction", JsonConvert.SerializeObject(transferResult), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						var tran = new TblTransaction
						{
							Id = Guid.NewGuid(),
							TranAmout = payload.Amount,
							DestinationAcctName = payload.DestinationAccountName,
							DestinationAcctNo = payload.DestinationAccountNumber,
							DesctionationBank = "Parallex Bank",
							TranType = transactionType,
							TransactionStatus = nameof(TransactionStatus.Failed),
							Narration = narration,
							SourceAccountName = payload.SourceAccountName,
							SourceAccountNo = payload.SourceAccountNumber,
							SourceBank = "Parallex Bank",
							CustAuthId = CorporateProfile.Id,
							Channel = "WEB",
							TransactionReference = transactionReference,
							SessionId = transferResult.TransactionReference,
							TranDate = date,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							BatchId = batchId,
						};
						var audit = new TblAuditTrail
						{
							Id = Guid.NewGuid(),
							ActionCarriedOut = nameof(AuditTrailAction.Intra_Bank_Transfer).Replace("_", " "),
							Ipaddress = payload.IPAddress,
							Macaddress = payload.MACAddress,
							HostName = payload.HostName,
							ClientStaffIpaddress = payload.ClientStaffIPAddress,
							NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + " to " + transferResult.AccountCredited,
							PreviousFieldValue = "",
							TransactionId = transactionReference,
							UserId = CorporateProfile.Id,
							Username = CorporateProfile.Username,
							Description = "Intrabank transfer",
							TimeStamp = DateTime.Now
						};
						pendingTranLog.Status = 2;
						pendingTranLog.TransactionStatus = 2;
						pendingTranLog.ApprovalStatus = 2;
						pendingCreditLog.CreditDate = date;
						pendingCreditLog.CreditStatus = 2;
						pendingCreditLog.TransactionReference = transactionReference;
						pendingCreditLog.SessionId = transferResult.TransactionReference;
						pendingCreditLog.TransactionResponseCode = transferResult.ResponseCode;
						pendingCreditLog.TransactionResponseMessage = transferResult.ResponseDescription;

						pendingTranLog.TransactionReference = transactionReference;
						pendingTranLog.SessionId = transferResult.TransactionReference;
						UnitOfWork.PendingTranLogRepo.Add(pendingTranLog);
						UnitOfWork.PendingCreditLogRepo.Add(pendingCreditLog);
						UnitOfWork.TransactionRepo.Add(tran);
						UnitOfWork.AuditTrialRepo.Add(audit);
						UnitOfWork.Complete();
						return BadRequest($"Intra bank Transfer Service API Error {transferResult.ResponseDescription}");
					}
					var Tranxt = new TblTransaction
					{
						Id = Guid.NewGuid(),
						TranAmout = payload.Amount,
						DestinationAcctName = transferResult.BeneficiaryName,
						DestinationAcctNo = transferResult.AccountCredited,
						DesctionationBank = "Parallex Bank",
						TranType = transactionType,
						TransactionStatus = nameof(TransactionStatus.Successful),
						Narration = narration,
						SourceAccountName = transferResult.SenderName,
						SourceAccountNo = transferResult.AccountDebited,
						SourceBank = "Parallex Bank",
						CustAuthId = CorporateProfile.Id,
						Channel = "WEB",
						TransactionReference = transactionReference,
						SessionId = transferResult.TransactionReference,
						TranDate = date,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						BatchId = batchId,
					};
					var auditTrail = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Intra_Bank_Transfer).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
						PreviousFieldValue = "",
						TransactionId = transactionReference,
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Intrabank transfer",
						TimeStamp = DateTime.Now
					};
					pendingTranLog.Status = 1;
					pendingTranLog.TransactionStatus = 1;
					pendingCreditLog.CreditStatus = 1;
					pendingCreditLog.TransactionReference = transactionReference;
					pendingCreditLog.SessionId = transferResult.TransactionReference;
					pendingCreditLog.TransactionResponseCode = transferResult.ResponseCode;
					pendingCreditLog.TransactionResponseMessage = transferResult.ResponseCode;
					pendingTranLog.TransactionReference = transactionReference;
					pendingTranLog.SessionId = transferResult.TransactionReference;
					pendingTranLog.ApprovalStatus = 1;
					pendingCreditLog.CreditDate = date;
					UnitOfWork.PendingTranLogRepo.Add(pendingTranLog);
					UnitOfWork.PendingCreditLogRepo.Add(pendingCreditLog);
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.TransactionRepo.Add(Tranxt);
					UnitOfWork.TransactionHistoryRepo.SetOrUpdateDailySingleTransLimitHistory(tblCorporateCustomer, (decimal)pendingTranLog.DebitAmount);
					UnitOfWork.Complete();
					isSuccessful = true;
					var recepitInfo = this.GetTransactionRecipt(pendingTranLog, pendingCreditLog);
					return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful", data = recepitInfo });
				}
				else
				{
					var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(payload.WorkflowId);
					pendingTranLog.Id = Guid.NewGuid();
					pendingTranLog.BatchId = batchId;
					pendingTranLog.ApprovalCount = workflowHierarchies.Count;
					pendingTranLog.ApprovalStage = 1;
					pendingTranLog.ApprovalStatus = 0;
					pendingTranLog.CompanyId = tblCorporateCustomer.Id;
					pendingTranLog.TransferType = transactionType.Replace("_", " ");
					pendingTranLog.DateInitiated = date;
					pendingTranLog.TransactionStatus = 0;
					pendingTranLog.Narration = payload.Narration;
					pendingTranLog.DebitAccountName = senderInfo.AccountName;
					pendingTranLog.DebitAccountNumber = senderInfo.AccountNumber;
					pendingTranLog.DebitAmount = payload.Amount;
					pendingTranLog.InitiatorId = CorporateProfile.Id;
					pendingTranLog.NoOfCredits = 1;
					pendingTranLog.OriginatorBvn = senderInfo.Bvn;
					pendingTranLog.Status = 0;
					pendingTranLog.Currency = "NGN";
					pendingTranLog.WorkflowId = payload.WorkflowId;
					pendingTranLog.TransactionLocation = payload.TransactionLocation;
					pendingTranLog.TransactionReference = transactionReference;

					foreach (var item in workflowHierarchies)
					{
						int toApproved = item.AuthorizationLevel == 1 ? 1 : 0;
						var corporateApprovalHistory = new TblCorporateApprovalHistory
						{
							Id = Guid.NewGuid(),
							LogId = pendingTranLog.Id,
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
					//create credit log
					pendingCreditLog.Id = Guid.NewGuid();
					pendingCreditLog.CreditDate = date;
					pendingCreditLog.TranLogId = pendingTranLog.Id;
					pendingCreditLog.CreditAccountNumber = receiverInfo.AccountNumber;
					pendingCreditLog.CreditAccountName = receiverInfo.AccountName;
					pendingCreditLog.CreditAmount = payload.Amount;
					pendingCreditLog.CreditStatus = 0;
					pendingCreditLog.CreditBankCode = parallexBankCode;
					pendingCreditLog.CreditBankName = "Parralex Bank";
					pendingCreditLog.BankVerificationNo = receiverInfo.BVN;
					pendingCreditLog.KycLevel = receiverInfo.KYCLevel;
					pendingCreditLog.NameEnquiryRef = receiverInfo.RequestId;
					pendingCreditLog.ResponseCode = receiverInfo.ResponseCode;
					pendingCreditLog.ResponseMessage = receiverInfo.ResponseMessage;
					pendingCreditLog.ChannelCode = "2";
					pendingCreditLog.Narration = payload.Narration;
					pendingCreditLog.CorporateCustomerId = tblCorporateCustomer.Id;
					pendingCreditLog.TransactionReference = transactionReference;


					var auditTrail = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Intra_Bank_Transfer).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated transfer of" + pendingTranLog.DebitAmount + "to " + pendingCreditLog.CreditAccountNumber,
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Corporate User Initiated Intra bank transfer",
						TimeStamp = DateTime.Now
					};
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.PendingTranLogRepo.Add(pendingTranLog);
					UnitOfWork.CorporateApprovalHistoryRepo.AddRange(tblCorporateApprovalHistories);
					UnitOfWork.PendingCreditLogRepo.Add(pendingCreditLog);
					UnitOfWork.Complete();
					isSuccessful = true;
				}

				var firstApproval = tblCorporateApprovalHistories.First(ctx => ctx.ApprovalLevel == 1);
				var initiatorName = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog.InitiatorId);
				var corporateUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)firstApproval?.UserId);
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(corporateUser.Email, initiatorName.FullName, $"{payload.Amount:0.00}")));
				return Ok(isSuccessful);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[ProducesResponseType(StatusCodes.Status201Created)]
		[HttpPost("VerifyInterBankTransaction")]
		public async Task<ActionResult<bool>> VerifyInterBankTransaction(InterBankTransactionDto model)
		{
			try
			{

				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
				}
				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

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
						LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var payload = new InterBankTransaction
				{
					Amount = Encryption.DecryptDecimals(model.Amount),
					DestinationAccountName = Encryption.DecryptStrings(model.DestinationAccountName),
					SourceAccountName = Encryption.DecryptStrings(model.SourceAccountName),
					SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
					DestinationBankCode = Encryption.DecryptStrings(model.DestinationBankCode),
					DestinationBankName = Encryption.DecryptStrings(model.DestinationBankName),
					Narration = Encryption.DecryptStrings(model.Narration),
					SourceBankName = Encryption.DecryptStrings(model.SourceBankName),
					TransactionType = Encryption.DecryptStrings(model.TransactionType),
					WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
					Otp = Encryption.DecryptStrings(model.Otp),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress)
				};

				try
				{
					payload.DestinationAccountNumber = Encryption.DecryptDebitAccount(model.DestinationAccountNumber);
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("Padding is invalid and cannot be removed"))
					{
						payload.Otp = ""; payload.TransactionLocation = "";
						LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "beneficiary Account Number could not be verify", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("beneficiary Account Number could not be verify");
					}
					return StatusCode(500, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
				}

				if (tblCorporateCustomer.MinAccountLimit > payload.Amount)
				{
					payload.Otp = ""; payload.TransactionLocation = "";
					var errorMessage = $"Transcation amount {payload.Amount} is below the Minimum transaction amount  {tblCorporateCustomer.MinAccountLimit} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}

				if (tblCorporateCustomer.MaxAccountLimit < payload.Amount)
				{
					payload.Otp = ""; payload.TransactionLocation = "";
					var errorMessage = $"Transcation amount {payload.Amount} has exceeded the maximum transaction amount {tblCorporateCustomer.MaxAccountLimit} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}

				var validateRelatedAccount = await this.ValidateRelatedAccount(tblCorporateCustomer, payload.SourceAccountNumber);
				if (!validateRelatedAccount.Status)
				{
					return BadRequest(validateRelatedAccount.Message);
				}

				if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
				{
					if (_auth != AuthorizationType.Single_Signatory)
					{
						if (CorporateProfile.ApprovalLimit < payload.Amount)
						{
							payload.Otp = ""; payload.TransactionLocation = "";
							var errorMessage = $"Transaction amount {payload.Amount} has exceeded your transaction limit {CorporateProfile.ApprovalLimit}";
							LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(errorMessage);
						}

						if (model.WorkflowId == null)
						{
							payload.Otp = ""; payload.TransactionLocation = "";
							LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Workflow is required", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Workflow is required");
						}

						var workflowValidation = ValidateWorkflowAccess(payload.WorkflowId, payload.Amount);
						if (!workflowValidation.Status)
						{
							payload.Otp = ""; payload.TransactionLocation = "";
							LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", workflowValidation.Message, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(workflowValidation.Message);
						}
					}
				}
				else
				{
					payload.Otp = ""; payload.TransactionLocation = "";
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Authorization type could not be determined!!!");
				}
				var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);
				if (senderInfo.ResponseCode != "00")
				{
					payload.Otp = ""; payload.TransactionLocation = "";
					var errorMessage = $"Source account number could not be verified -> {senderInfo.ResponseDescription}";
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", errorMessage, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}
				if (senderInfo.AccountStatus != "A")
				{
					var errorMessage = $"Source account number is not Active, Transaction cannot be completed -> {senderInfo.AccountStatus}";
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", errorMessage, JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}

				if (senderInfo.FreezeCode != "N")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Source account number is on debit Freeze, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is on debit Freeze, transaction can not be completed");
				}

				if (senderInfo.CurrencyCode != "NGN")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Only naira Transaction is allow , transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Only naira Transaction is allow , transaction can not be completed");
				}

				if (senderInfo.Effectiveavail < payload.Amount)
				{
					payload.Otp = ""; payload.TransactionLocation = "";
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Insufficient funds", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Insufficient funds");
				}

				var payloadd = new InterbankNameEnquiryModel
				{
					accountNumber = payload.DestinationAccountNumber,
					BankCode = payload.DestinationBankCode,
				};
				var result = await _apiService.BankNameInquire(payloadd.accountNumber, payloadd.BankCode);
				if (result.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "VerifyInterBankTransaction", "Receiver account number could not be verified", JsonConvert.SerializeObject(result), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Receiver account number could not be verified");
				}

				var feeCharges = await UnitOfWork.NipsFeeChargeRepo.ListAllAsync();
				var nipsCharge = NipsCharge.Calculate(feeCharges, payload.Amount);
				var response = new VerifyBulkTransactionResponse
				{
					AuthLimit = tblCorporateCustomer.AuthenticationLimit != null ? 1 : 0,
					AuthLimitIsEnable = payload.Amount >= tblCorporateCustomer.AuthenticationLimit ? 1 : 0,
					Fee = nipsCharge.Fee,
					Vat = nipsCharge.Vat,
					TotalAmount = payload.Amount + nipsCharge.Fee + nipsCharge.Vat
				};
				return Ok(response);
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("InitiateInterBankTransaction")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> InitiateInterBankTransaction(IntraBankTransactionDto model)
		{
			try
			{
				string errormsg = string.Empty;
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

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
						LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var payload = new InterBankTransaction
				{
					Amount = Encryption.DecryptDecimals(model.Amount),
					DestinationAccountName = Encryption.DecryptStrings(model.DestinationAccountName),
					SourceAccountName = Encryption.DecryptStrings(model.SourceAccountName),
					SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
					DestinationBankCode = Encryption.DecryptStrings(model.DestinationBankCode),
					DestinationBankName = Encryption.DecryptStrings(model.DestinationBankName),
					Narration = Encryption.DecryptStrings(model.Narration),
					SourceBankName = Encryption.DecryptStrings(model.SourceBankName),
					TransactionType = Encryption.DecryptStrings(model.TransactionType),
					WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
					TransactionLocation = Encryption.DecryptStrings(model.TransactionLocation),
					Otp = Encryption.DecryptStrings(model.Otp),
					Pin = Encryption.DecryptStrings(model.Pin),
					AuthLimitIsEnable = Encryption.DecryptInt(model.AuthLimitIsEnable),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress)
				};


				var validator = new InitiaInterBankTransactionValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Entity Validation", JsonConvert.SerializeObject(results.Errors), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				try
				{
					payload.DestinationAccountNumber = Encryption.DecryptDebitAccount(model.DestinationAccountNumber);
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("Padding is invalid and cannot be removed"))
					{
						payload.Otp = "";
						LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "beneficiary Account Number could not be verify", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("beneficiary Account Number could not be verify");
					}
					return StatusCode(500, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
				}

				var userName = $"{CorporateProfile.Username}";
				var validOTP = await _2fa.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", $"2FA API ERROR:{validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(validOTP.ResponseMessage);
				}

				if (payload.AuthLimitIsEnable > 0)
				{
					var transPin = CorporateProfile.TranPin != null ? Encryption.DecriptPin(CorporateProfile.TranPin) : null;
					if (transPin == null)
					{
						return BadRequest("transaction can not be completed because transaction is not set");
					}
					else
					{
						if (transPin != payload.Pin)
						{
							LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", $"Invalid Pin", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Invalid Transaction pin");
						}
					}
				}

				if (tblCorporateCustomer.MaxAccountLimit < payload.Amount)
				{
					payload.Otp = "";
					var errorMessage = $"Transcation amount {payload.Amount} has exceeded the maximum transaction amount {tblCorporateCustomer.MaxAccountLimit} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}
				//check daily transfer limit
				if (tblCorporateCustomer.SingleTransDailyLimit < payload.Amount)
				{
					payload.Otp = "";
					var errorMessage = $"Transcation amount {Helper.formatCurrency(payload.Amount)} has exceeded the maximum daily transaction limit {Helper.formatCurrency(tblCorporateCustomer.SingleTransDailyLimit)} for your organisation";
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errorMessage);
				}

				var validateRelatedAccount = await this.ValidateRelatedAccount(tblCorporateCustomer, payload.SourceAccountNumber);
				if (!validateRelatedAccount.Status)
				{
					return BadRequest(validateRelatedAccount.Message);
				}

				var bankList = await _apiService.GetBanks();
				if (bankList.ResponseCode != "00")
				{
					return BadRequest(bankList.ResponseMessage);
				}

				//check cummulatve daily transfer limit
				if (DailyLimitExceeded(tblCorporateCustomer, payload.Amount, out errormsg))
				{
					payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", errormsg, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(errormsg);
				}

				if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
				{
					if (_auth != AuthorizationType.Single_Signatory)
					{
						if (CorporateProfile.ApprovalLimit < payload.Amount)
						{
							payload.Otp = "";
							var errorMessage = $"Transaction amount {payload.Amount} has exceeded your transaction limit {CorporateProfile.ApprovalLimit}";
							LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", errorMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(errorMessage);
						}

						if (payload.WorkflowId == default)
						{
							payload.Otp = "";
							LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Workflow is required", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("Workflow is required");
						}

						var validateWorkFlow = ValidateWorkflowAccess(payload.WorkflowId, payload.Amount);
						if (!validateWorkFlow.Status)
						{
							payload.Otp = "";
							LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", validateWorkFlow.Message, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest(validateWorkFlow.Message);
						}
					}
				}
				else
				{
					payload.Otp = "";
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Authorization type could not be determined!!!");
				}
				//call name inquiry API
				var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);
				if (senderInfo.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Source account number could not be verified", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number could not be verified");
				}

				if (senderInfo.AccountStatus != "A")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Source account number is not Active", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is not Active");
				}

				if (senderInfo.FreezeCode != "N")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Source account number is on debit Freeze, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Source account number is on debit Freeze, transaction can not be completed");
				}

				if (senderInfo.CurrencyCode != "NGN")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Only naira Transaction is allow , transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Only naira Transaction is allow , transaction can not be completed");
				}

				var receiverInfo = await _apiService.BankNameInquire(payload.DestinationAccountNumber, payload.DestinationBankCode);
				if (receiverInfo.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Receiver account number could not be verified", JsonConvert.SerializeObject(receiverInfo), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest("Receiver account number could not be verified");
				}

				var narration = $"IP|{tblCorporateCustomer.CompanyName}|{receiverInfo.AccountName}|{payload.Narration}";
				payload.Narration = narration;
				var getBank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode.Trim() == payload.DestinationBankCode.Trim());

				var tblCorporateApprovalHistories = new List<TblCorporateApprovalHistory>();
				var date = DateTime.Now;
				var pendingTranLog = new TblPendingTranLog();
				var pendingCreditLog = new TblPendingCreditLog();
				var corporateApprovalHistoryList = new List<TblCorporateApprovalHistory>();
				var transactionReference = Generate16DigitNumber.Create16DigitString();
				var batchid = Guid.NewGuid();

				string transactionType = payload.TransactionType?.Replace("_", " ");


				if (_auth == AuthorizationType.Single_Signatory)
				{
					if (senderInfo.Effectiveavail < payload.Amount)
					{
						payload.Otp = "";
						LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", "Insufficient funds", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Insufficient funds");
					}
					pendingTranLog.Id = Guid.NewGuid();
					pendingTranLog.BatchId = batchid;
					pendingTranLog.ApprovalCount = 1;
					pendingTranLog.ApprovalStage = 1;
					pendingTranLog.CompanyId = tblCorporateCustomer.Id;
					pendingTranLog.TransferType = transactionType;
					pendingTranLog.DateInitiated = date;
					pendingTranLog.Narration = payload.Narration;
					pendingTranLog.DebitAccountName = senderInfo.AccountName;
					pendingTranLog.DebitAccountNumber = senderInfo.AccountNumber;
					pendingTranLog.DebitAmount = payload.Amount;
					pendingTranLog.InitiatorId = CorporateProfile.Id;
					pendingTranLog.NoOfCredits = 1;
					pendingTranLog.OriginatorBvn = senderInfo.Bvn;
					pendingTranLog.Currency = "NGN";
					pendingTranLog.TransactionLocation = payload.TransactionLocation;

					pendingCreditLog.Id = Guid.NewGuid();
					pendingCreditLog.TranLogId = pendingTranLog.Id;
					pendingCreditLog.CreditAccountNumber = receiverInfo.AccountNumber;
					pendingCreditLog.CreditAccountName = receiverInfo.AccountName;
					pendingCreditLog.CreditAmount = payload.Amount;
					pendingCreditLog.CreditBankCode = getBank.InstitutionCode;
					pendingCreditLog.CreditBankName = getBank.InstitutionName;
					pendingCreditLog.BankVerificationNo = receiverInfo.BVN;
					pendingCreditLog.KycLevel = receiverInfo.KYCLevel;
					pendingCreditLog.NameEnquiryRef = receiverInfo.RequestId;
					pendingCreditLog.ResponseCode = receiverInfo.ResponseCode;
					pendingCreditLog.ResponseMessage = receiverInfo.ResponseMessage;
					pendingCreditLog.ChannelCode = "2";
					pendingCreditLog.Narration = payload.Narration;
					pendingCreditLog.CorporateCustomerId = tblCorporateCustomer.Id;
					pendingCreditLog.TryCount = 0;


					var transfer = new InterBankPostDto
					{
						accountToDebit = payload.SourceAccountNumber,
						userName = CorporateProfile.Username,
						channel = "2",
						transactionLocation = payload.TransactionLocation,
						interTransferDetails = new List<InterTransferDetail>
							{
								new InterTransferDetail
								{
									transactionReference = transactionReference,
									beneficiaryAccountName = receiverInfo.AccountName,
									beneficiaryAccountNumber = receiverInfo.AccountNumber,
									transactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
									amount = payload.Amount,
									customerRemark = payload.Narration,
									beneficiaryBVN = receiverInfo.BVN,
									beneficiaryKYC = receiverInfo.KYCLevel,
									beneficiaryBankCode = payload.DestinationBankCode,
									beneficiaryBankName = payload.DestinationBankName,
									nameEnquirySessionID = receiverInfo.RequestId
								}
							},

					};

					var transferResult = await _apiService.InterBankTransfer(transfer);
					if (transferResult.ResponseCode != "00")
					{
						LogFormater<TransactionController>.Error(_logger, "InitiateInterBankTransaction", $"Transaction Failed", JsonConvert.SerializeObject(transferResult), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));

						var tranxt = new TblTransaction
						{
							Id = Guid.NewGuid(),
							TranAmout = payload.Amount,
							DestinationAcctName = payload.DestinationAccountName,
							DestinationAcctNo = payload.DestinationAccountNumber,
							DesctionationBank = payload.DestinationBankName,
							TranType = transactionType,
							TransactionStatus = nameof(TransactionStatus.Failed),
							Narration = payload.Narration,
							SourceAccountName = payload.SourceAccountName,
							SourceAccountNo = payload.SourceAccountNumber,
							SourceBank = "Parallex Bank",
							CustAuthId = CorporateProfile.Id,
							Channel = "WEB",
							TransactionReference = transactionReference,
							SessionId = transferResult.TransactionReference,
							TranDate = date,
							CorporateCustomerId = CorporateProfile.CorporateCustomerId,
							BatchId = batchid
						};
						var auditTraill = new TblAuditTrail
						{
							Id = Guid.NewGuid(),
							ActionCarriedOut = nameof(AuditTrailAction.Inter_Bank_Transfer).Replace("_", " "),
							Ipaddress = payload.IPAddress,
							Macaddress = payload.MACAddress,
							HostName = payload.HostName,
							ClientStaffIpaddress = payload.ClientStaffIPAddress,
							NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
							PreviousFieldValue = "",
							TransactionId = transactionReference,
							UserId = CorporateProfile.Id,
							Username = CorporateProfile.Username,
							Description = "Intrabank transfer",
							TimeStamp = DateTime.Now
						};
						pendingTranLog.Status = 2;
						pendingTranLog.TransactionStatus = 2;
						pendingTranLog.ApprovalStatus = 2;
						pendingCreditLog.CreditDate = date;
						pendingCreditLog.CreditStatus = 2;
						pendingCreditLog.TransactionResponseCode = transferResult.ResponseCode;
						pendingCreditLog.TransactionResponseMessage = transferResult.ResponseDescription;
						pendingCreditLog.TransactionReference = transactionReference;
						pendingCreditLog.SessionId = transferResult.TransactionReference;
						pendingTranLog.TransactionReference = transactionReference;
						pendingTranLog.SessionId = transferResult.TransactionReference;
						UnitOfWork.PendingTranLogRepo.Add(pendingTranLog);
						UnitOfWork.PendingCreditLogRepo.Add(pendingCreditLog);
						UnitOfWork.TransactionRepo.Add(tranxt);
						UnitOfWork.AuditTrialRepo.Add(auditTraill);
						UnitOfWork.Complete();
						return BadRequest($"Inter bank Transfer Service API Error {transferResult.ResponseDescription}");
					}

					var Tranxt = new TblTransaction
					{
						Id = Guid.NewGuid(),
						TranAmout = payload.Amount,
						DestinationAcctName = transferResult.BeneficiaryName,
						DestinationAcctNo = transferResult.AccountCredited,
						DesctionationBank = payload.DestinationBankName,
						TranType = transactionType,
						TransactionStatus = nameof(TransactionStatus.Successful),
						Narration = payload.Narration,
						SourceAccountName = transferResult.SenderName,
						SourceAccountNo = transferResult.AccountDebited,
						SourceBank = "Parallex Bank",
						CustAuthId = CorporateProfile.Id,
						Channel = "WEB",
						TransactionReference = transactionReference,
						SessionId = transferResult.TransactionReference,
						TranDate = date,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId,
						BatchId = batchid,
					};
					var AuditTrail = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Inter_Bank_Transfer).Replace("_", " "),
						Ipaddress = payload.IPAddress,
						Macaddress = payload.MACAddress,
						HostName = payload.HostName,
						ClientStaffIpaddress = payload.ClientStaffIPAddress,
						NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
						PreviousFieldValue = "",
						TransactionId = Tranxt.Id.ToString(),
						UserId = CorporateProfile.Id,
						Username = CorporateProfile.Username,
						Description = "Interbank transfer",
						TimeStamp = DateTime.Now
					};
					pendingTranLog.Status = 1;
					pendingTranLog.TransactionStatus = 1;
					pendingCreditLog.CreditStatus = 1;
					pendingTranLog.ApprovalStatus = 1;
					pendingCreditLog.CreditDate = date;
					pendingCreditLog.TransactionReference = transactionReference;
					pendingCreditLog.SessionId = transferResult.TransactionReference;
					pendingCreditLog.TransactionResponseCode = transferResult.ResponseCode;
					pendingCreditLog.TransactionResponseMessage = transferResult.ResponseDescription;

					pendingTranLog.TransactionReference = transactionReference;
					pendingTranLog.SessionId = transferResult.TransactionReference;
					UnitOfWork.PendingTranLogRepo.Add(pendingTranLog);
					UnitOfWork.PendingCreditLogRepo.Add(pendingCreditLog);
					UnitOfWork.AuditTrialRepo.Add(AuditTrail);
					UnitOfWork.TransactionRepo.Add(Tranxt);
					UnitOfWork.TransactionHistoryRepo.SetOrUpdateDailySingleTransLimitHistory(tblCorporateCustomer, (decimal)pendingTranLog.DebitAmount);
					UnitOfWork.Complete();
					var recepitInfo = this.GetTransactionRecipt(pendingTranLog, pendingCreditLog);
					return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful", data = recepitInfo });
				}

				var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId((Guid)payload.WorkflowId);
				pendingTranLog.Id = Guid.NewGuid();
				pendingTranLog.BatchId = batchid;
				pendingTranLog.ApprovalCount = workflowHierarchies.Count;
				pendingTranLog.ApprovalStage = 1;
				pendingTranLog.ApprovalStatus = 0;
				pendingTranLog.CompanyId = tblCorporateCustomer.Id;
				pendingTranLog.TransferType = transactionType.Replace("_", " ");
				pendingTranLog.DateInitiated = date;
				pendingTranLog.TransactionStatus = 0;
				pendingTranLog.Narration = payload.Narration;
				pendingTranLog.DebitAccountName = senderInfo.AccountName;
				pendingTranLog.DebitAccountNumber = senderInfo.AccountNumber;
				pendingTranLog.DebitAmount = payload.Amount;
				pendingTranLog.InitiatorId = CorporateProfile.Id;
				pendingTranLog.NoOfCredits = 1;
				pendingTranLog.OriginatorBvn = senderInfo.Bvn;
				pendingTranLog.Status = 0;
				pendingTranLog.Currency = "NGN";
				pendingTranLog.WorkflowId = payload.WorkflowId;
				pendingTranLog.TransactionLocation = payload.TransactionLocation;

				foreach (var item in workflowHierarchies)
				{
					int toApproved = item.AuthorizationLevel == 1 ? 1 : 0;
					var corporateApprovalHistory = new TblCorporateApprovalHistory
					{
						Id = Guid.NewGuid(),
						LogId = pendingTranLog.Id,
						Status = nameof(AuthorizationStatus.Pending),
						ApprovalLevel = item.AuthorizationLevel,
						ApproverName = item.ApproverName,
						Description = $"Authorizer {item.AuthorizationLevel}",
						Comment = "",
						UserId = item.ApproverId,
						ToApproved = toApproved,
						CorporateCustomerId = CorporateProfile.CorporateCustomerId
					};
					tblCorporateApprovalHistories.Add(corporateApprovalHistory);
				}
				//create credit log
				pendingCreditLog.Id = Guid.NewGuid();
				pendingCreditLog.TranLogId = pendingTranLog.Id;
				pendingCreditLog.CreditAccountNumber = receiverInfo.AccountNumber;
				pendingCreditLog.CreditAccountName = receiverInfo.AccountName;
				pendingCreditLog.CreditAmount = payload.Amount;
				pendingCreditLog.CreditStatus = 0;
				pendingCreditLog.CreditBankCode = getBank.InstitutionCode;
				pendingCreditLog.CreditBankName = getBank.InstitutionName;
				pendingCreditLog.BankVerificationNo = receiverInfo.BVN;
				pendingCreditLog.KycLevel = receiverInfo.KYCLevel;
				pendingCreditLog.NameEnquiryRef = receiverInfo.RequestId;
				pendingCreditLog.ResponseCode = receiverInfo.ResponseCode;
				pendingCreditLog.ResponseMessage = receiverInfo.ResponseMessage;
				pendingCreditLog.ChannelCode = "2";
				pendingCreditLog.Narration = payload.Narration;
				pendingCreditLog.TryCount = 0;

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Inter_Bank_Transfer).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + " Initiated transfer of " + pendingTranLog.DebitAmount + " to " + pendingCreditLog.CreditAccountNumber,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Corporate User Initiated Interbank transfer",
					TimeStamp = DateTime.Now
				};

				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.PendingTranLogRepo.Add(pendingTranLog);
				UnitOfWork.CorporateApprovalHistoryRepo.AddRange(tblCorporateApprovalHistories);
				UnitOfWork.PendingCreditLogRepo.Add(pendingCreditLog);
				UnitOfWork.Complete();

				var firstApproval = tblCorporateApprovalHistories.First(ctx => ctx.ApprovalLevel == 1);
				var CorporateUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync(firstApproval.UserId.Value);
				var initiatorName = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog.InitiatorId);

				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(CorporateUser.Email, initiatorName.FullName, string.Format("{0:0.00}", payload.Amount))));
				return Ok(new { Responsecode = "00", ResponseDescription = "Transaction has been forwarded for approval" });
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("ApproveTransaction")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<bool>> ApproveTransaction(ApproveTransactionDto model)
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
				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				var payload = new ApproveTransactionModel
				{
					AuthorizerId = Encryption.DecryptGuid(model.AuthorizerId),
					Comment = Encryption.DecryptStrings(model.Comment),
					Otp = Encryption.DecryptStrings(model.Otp),
					TranLogId = Encryption.DecryptGuid(model.TranLogId),
					Pin = Encryption.DecryptStrings(model.Pin),
					AuthLimitIsEnable = Encryption.DecryptInt(model.AuthLimitIsEnable),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
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
				var validOTP = await _2fa.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", $"2FA API ERROR: {validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName), "");
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
							return BadRequest("Invalid Transaction pin");
						}
					}
				}



				var pendingTranLog = UnitOfWork.PendingTranLogRepo.GetByIdAsync(payload.TranLogId);
				if (pendingTranLog == null)
				{
					LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Invalid transactionLogId", JsonConvert.SerializeObject(corporateCustomer.CustomerId), "");
					return BadRequest("Invalid transaction log id");
				}

				if (pendingTranLog.Status != 0)
				{
					LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Transaction is no longer pending approval", JsonConvert.SerializeObject(corporateCustomer.CustomerId), "");
					return BadRequest("Transaction is no longer pending approval");
				}
				var pendingCreditLog = UnitOfWork.PendingCreditLogRepo.GetPendingCreditTranLogByTranLogId(pendingTranLog.Id);
				var corporateApprovalHistory = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryByAuthId(CorporateProfile.Id, pendingTranLog.Id);
				var auditTrail = new TblAuditTrail();
				if (corporateApprovalHistory == null)
				{
					LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Corporate approval history could not be retrieved", JsonConvert.SerializeObject(corporateCustomer.CustomerId), "");
					return BadRequest("Corporate approval history could not be retrieved");
				}

				var date = DateTime.Now;
				bool isSuccessful;
				auditTrail.Ipaddress = payload.IPAddress;
				auditTrail.ClientStaffIpaddress = payload.ClientStaffIPAddress;
				auditTrail.HostName = payload.HostName;
				auditTrail.Macaddress = payload.MACAddress;

				if (DailyLimitExceeded(corporateCustomer, (decimal)pendingTranLog.DebitAmount, out string errorMessage))
				{
					return BadRequest(errorMessage);
				}

				if (pendingTranLog.ApprovalCount == pendingTranLog.ApprovalStage)
				{
					corporateApprovalHistory.Status = nameof(AuthorizationStatus.Approved);
					corporateApprovalHistory.ApprovalDate = date;
					corporateApprovalHistory.Comment = payload.Comment;
					corporateApprovalHistory.ToApproved = 0;

					var tblPendingCreditLog = UnitOfWork.PendingCreditLogRepo.GetPendingCreditTranLogByTranLogId(pendingTranLog.Id);
					if (tblPendingCreditLog == null)
					{
						LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Credit log info could not be retrieved", JsonConvert.SerializeObject(corporateCustomer.CustomerId), "");
						return BadRequest("Credit log info could not be retrieved");
					}

					var senderInfo = await _apiService.CustomerNameInquiry(pendingTranLog.DebitAccountNumber);
					if (senderInfo.ResponseCode != "00")
					{
						LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Source account number could not be verified", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Source account number could not be verified");
					}

					if (senderInfo.AccountStatus != "A")
					{
						LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Source account number is not Active", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Source account number is not Active");
					}

					if (senderInfo.FreezeCode != "N")
					{
						LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Source account number is on debit Freeze, transaction can not be completed", JsonConvert.SerializeObject(senderInfo), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Source account number is on debit Freeze, transaction can not be completed");
					}

					if (senderInfo.Effectiveavail < pendingTranLog.DebitAmount)
					{
						payload.Otp = "";
						LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Insufficient funds", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Insufficient funds");
					}

					if (pendingTranLog.TransferType.Replace(" ", "_") == nameof(TransactionType.Own_Transfer) || pendingTranLog.TransferType.Replace(" ", "_") == nameof(TransactionType.Intra_Bank_Transfer))
					{
						var resultt = await PostIntraBankTransfer(tblPendingCreditLog, pendingTranLog, date, transactionReference, auditTrail);
						if (resultt.ResponseCode != "00")
						{
							LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Failed Transaction", JsonConvert.SerializeObject(resultt), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
							return BadRequest(resultt.ResponseDescription);
						}
						pendingTranLog.TransactionStatus = 1;
						pendingTranLog.ApprovalStatus = 1;
						pendingTranLog.Status = 1;
						pendingTranLog.DateApproved = date;
						pendingCreditLog.CreditDate = date;
						pendingCreditLog.CreditStatus = 1;

						pendingCreditLog.TransactionReference = resultt.TransactionReference;
						pendingTranLog.TransactionReference = resultt.TransactionReference;
						pendingCreditLog.CorporateCustomerId = corporateCustomer.Id;
						UnitOfWork.TransactionHistoryRepo.SetOrUpdateDailySingleTransLimitHistory(corporateCustomer, (decimal)pendingTranLog.DebitAmount);
						UnitOfWork.PendingTranLogRepo.UpdatePendingTranLog(pendingTranLog);
						UnitOfWork.PendingCreditLogRepo.UpdatePendingCreditLog(pendingCreditLog);
						UnitOfWork.Complete();
						isSuccessful = true;
						var recepitInfo = this.GetTransactionRecipt(pendingTranLog, pendingCreditLog);
						return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful", data = recepitInfo });
					}

					var result = await PostInterBankTransfer(tblPendingCreditLog, pendingTranLog, date, transactionReference, auditTrail);
					if (result.ResponseCode != "00")
					{
						LogFormater<TransactionController>.Error(_logger, "ApproveTransaction", "Failed Transaction", JsonConvert.SerializeObject(result), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest(result.ResponseDescription);
						// pendingTranLog.Status = 2;
						// pendingTranLog.TransactionStatus = 2;
						// pendingTranLog.ApprovalStatus = 2;
						// pendingTranLog.Status = 2;
						// pendingCreditLog.CreditDate = date;
						// pendingCreditLog.CreditStatus = 2;
						// pendingCreditLog.TransactionReference = tblPendingCreditLog.TransactionReference;
						// pendingCreditLog.SessionId = result.TransactionReference;
						// pendingTranLog.TransactionReference = result.TransactionReference;
						// pendingTranLog.TransactionReference = tblPendingCreditLog.TransactionReference;
						// pendingCreditLog.CorporateCustomerId = corporateCustomer.Id;
						// isSuccessful = false;
						// UnitOfWork.PendingTranLogRepo.UpdatePendingTranLog(pendingTranLog);
						// UnitOfWork.PendingCreditLogRepo.UpdatePendingCreditLog(pendingCreditLog);
						// UnitOfWork.Complete();
						// return BadRequest(result.ResponseDescription);
					}
					pendingTranLog.TransactionStatus = 1;
					pendingTranLog.ApprovalStatus = 1;
					pendingTranLog.Status = 1;
					pendingTranLog.DateApproved = date;
					pendingCreditLog.CreditDate = date;
					pendingCreditLog.CreditStatus = 1;
					pendingCreditLog.TransactionReference = tblPendingCreditLog.TransactionReference;
					pendingCreditLog.SessionId = result.TransactionReference;
					pendingCreditLog.CorporateCustomerId = corporateCustomer.Id;
					pendingTranLog.SessionId = result.TransactionReference;
					pendingTranLog.TransactionReference = tblPendingCreditLog.TransactionReference;
					isSuccessful = true;
					UnitOfWork.TransactionHistoryRepo.SetOrUpdateDailySingleTransLimitHistory(corporateCustomer, (decimal)pendingTranLog.DebitAmount);
					UnitOfWork.PendingTranLogRepo.UpdatePendingTranLog(pendingTranLog);
					UnitOfWork.PendingCreditLogRepo.UpdatePendingCreditLog(pendingCreditLog);
					isSuccessful = true;
					UnitOfWork.Complete();
					return Ok(isSuccessful);
				}

				pendingTranLog.ApprovalStage += 1;
				corporateApprovalHistory.Status = nameof(AuthorizationStatus.Approved);
				corporateApprovalHistory.ToApproved = 0;
				corporateApprovalHistory.ApprovalDate = date;
				corporateApprovalHistory.Comment = payload.Comment;
				//update tables
				UnitOfWork.PendingTranLogRepo.UpdatePendingTranLog(pendingTranLog);
				UnitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(corporateApprovalHistory);
				isSuccessful = true;
				UnitOfWork.Complete();
				var newTranApprover = UnitOfWork.CorporateApprovalHistoryRepo.GetNextApproval(pendingTranLog);
				if (newTranApprover != null)
				{
					newTranApprover.ToApproved = 1;
					UnitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(newTranApprover);
					UnitOfWork.Complete();

					var approvalInfo = UnitOfWork.CorporateProfileRepo.GetByIdAsync(CorporateProfile.Id);
					var initiatorInfo = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog?.InitiatorId);

					var dto = new EmailNotification
					{
						Action = nameof(AuthorizationStatus.Approved),
						Amount = string.Format("{0:0.00}", pendingTranLog.DebitAmount)
					};
					notify.NotifyCorporateTransfer(initiatorInfo, approvalInfo, dto, payload.Comment);
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
				var transactionReference = Generate16DigitNumber.Create16DigitString();
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (corporateCustomer == null)
				{
					return BadRequest("Invalid corporate customer id");
				}

				var userName = $"{CorporateProfile.Username}";
				var validOTP = await _2fa.TokenAuth(userName, payload.Otp);
				if (validOTP.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "DeclineTransaction", $"2FA API ERROR: {validOTP.ResponseMessage}", JsonConvert.SerializeObject(userName));
					return BadRequest(validOTP.ResponseMessage);
				}
				var pendingTranLog = UnitOfWork.PendingTranLogRepo.GetByIdAsync(payload.TranLogId);
				if (pendingTranLog == null)
				{
					LogFormater<TransactionController>.Error(_logger, "DeclineTransaction", "Invalid transaction log id", JsonConvert.SerializeObject(userName));
					return BadRequest("Invalid transaction log id");
				}

				if (pendingTranLog.Status != (int)ProfileStatus.Pending)
				{
					LogFormater<TransactionController>.Error(_logger, "DeclineTransaction", "Transaction is no longer pending approval", JsonConvert.SerializeObject(userName));
					return BadRequest("Transaction is no longer pending approval");
				}

				if (pendingTranLog.Status == (int)ProfileStatus.Declined)
				{
					LogFormater<TransactionController>.Error(_logger, "DeclineTransaction", "Transaction is already decline", JsonConvert.SerializeObject(userName));
					return BadRequest("Transaction is already decline");
				}

				var pendingCreditLog = UnitOfWork.PendingCreditLogRepo.GetPendingCreditTranLogByTranLogId(payload.TranLogId);


				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Decline transfer of " + pendingTranLog.DebitAmount + " to " + pendingCreditLog.CreditAccountNumber,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = $"Corporate Authorizer Decline {pendingTranLog.TransferType} Transaction reason been: {payload.Comment}",
					TimeStamp = DateTime.Now
				};

				var corporateApprovalHistory = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryByAuthId(CorporateProfile.Id, pendingTranLog.Id);
				pendingTranLog.Status = (int)ProfileStatus.Declined;
				pendingTranLog.TransactionStatus = (int)ProfileStatus.Declined;
				corporateApprovalHistory.Status = nameof(AuthorizationStatus.Decline);
				corporateApprovalHistory.ToApproved = 0;
				corporateApprovalHistory.ApprovalDate = DateTime.Now;
				corporateApprovalHistory.Comment = payload.Comment;
				//update tables
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.PendingTranLogRepo.UpdatePendingTranLog(pendingTranLog);
				UnitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(corporateApprovalHistory);
				UnitOfWork.Complete();

				var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog?.InitiatorId);

				var dto = new EmailNotification
				{
					Action = nameof(AuthorizationStatus.Decline),
					Amount = string.Format("{0:0.00}", pendingTranLog.DebitAmount)
				};
				notify.NotifyCorporateTransfer(initiatorProfile, null, dto, payload.Comment);

				return Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("TransactionHistory")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblTransaction>> TransactionHistory()
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
				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewTransactionHistory))
				{
					if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
					{
						if (_authType != AuthorizationType.Single_Signatory)
						{
							LogFormater<TransactionController>.Error(_logger, "TransactionHistory", "UnAuthorized Access", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<TransactionController>.Error(_logger, "TransactionHistory", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}
				var singleTransaction = UnitOfWork.PendingCreditLogRepo.GetCompanyCreditTranLogs(tblCorporateCustomer.Id);
				return singleTransaction == null ? Ok(singleTransaction) : Ok(singleTransaction.OrderByDescending(ctx => ctx.Sn));
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));

			}
		}

		[HttpGet("PendingTransactionLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblPendingTranLog>> PendingTransactionLogs()
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
							LogFormater<TransactionController>.Error(_logger, "PendingTransactionLogs", "UnAuthorized Access", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<TransactionController>.Error(_logger, "PendingTransactionLogs", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var pendingTranLogs = UnitOfWork.PendingTranLogRepo.GetAllCompanyPendingTranLog((Guid)CorporateProfile.CorporateCustomerId).ToList();
				var transactionHistory = new List<ApproveTransactionResponse>();
				foreach (var item in pendingTranLogs)
				{
					var response = new ApproveTransactionResponse
					{
						AuthLimit = tblCorporateCustomer.AuthenticationLimit != null ? 1 : 0,
						AuthLimitIsEnable = item.DebitAmount >= tblCorporateCustomer.AuthenticationLimit ? 1 : 0,
						PendingTransaction = item,
						Sn = item.Sn,
					};
					transactionHistory.Add(response);
				}
				return transactionHistory?.Count > 0 ? Ok(transactionHistory.OrderByDescending(x => x.Sn)) : Ok(transactionHistory);
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

				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
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
							LogFormater<TransactionController>.Error(_logger, "DeclineTransactions", "UnAuthorized Access", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<TransactionController>.Error(_logger, "DeclineTransactions", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var pendingTranLogs = UnitOfWork.PendingTranLogRepo.GetAllDeclineTransaction((Guid)CorporateProfile.CorporateCustomerId).ToList();

				if (pendingTranLogs != null && pendingTranLogs?.Count > 0)
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

		[HttpGet("PendingCreditLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblPendingCreditLog>> PendingCreditLogs(string transLogId)
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

				var tblCorporateCustomer =
					UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewPendingTransaction))
				{
					if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"),
								out AuthorizationType _authType))
					{
						if (_authType != AuthorizationType.Single_Signatory)
						{
							return BadRequest("UnAuthorized Access");
						}
					}
					else
					{
						LogFormater<TransactionController>.Error(_logger, "PendingCreditLogs", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}

				var transactionId = Encryption.DecryptGuid(transLogId);
				var pendingTranLogs = UnitOfWork.PendingCreditLogRepo.GetPendingCreditTranLogsByTranLogId(transactionId);
				return pendingTranLogs?.Count > 0 ? Ok(pendingTranLogs.OrderByDescending(x => x.Sn)) : Ok(pendingTranLogs);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR,
					responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500,
					new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR,
						responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("InterbankNameInquiry")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<ResponseDTO<InterbankNameEnquiryResponseDto>>> InterbankNameInquiry(InterbankNameEnquiryFormModel model)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (model == null)
				{
					return BadRequest("Model is empty");
				}

				if (string.IsNullOrEmpty(model.accountNumber))
				{
					return BadRequest("Account number is required!!!");
				}

				if (string.IsNullOrEmpty(model.destinationBankCode))
				{
					return BadRequest("Bank code is required!!!");
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}
				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);

				var payload = new InterbankNameEnquiryModel
				{
					accountNumber = Encryption.DecryptStrings(model.accountNumber),
					BankCode = Encryption.DecryptStrings(model.destinationBankCode)
				};
				//call name inquiry API
				var result = await _apiService.BankNameInquire(payload.accountNumber, payload.BankCode);
				if (result.ResponseCode != "00")
				{
					LogFormater<TransactionController>.Error(_logger, "InterbankNameInquiry", result.ResponseMessage, JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
					return BadRequest(result.ResponseMessage);
				}

				return Ok(new ResponseDTO<InterbankNameEnquiryResponseDto>(_data: result, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				if (ex.InnerException != null)
				{
					return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false));
				}
				return StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));

			}
		}

		[HttpPost("InterbankBankList")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<ListResponseDTO<BankListResponseData>>> InterbankBankList()
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
				}

				//call api
				var bankList = await _apiService.GetBanks();
				return Ok(new ResponseDTO<BankListResponseData>(_data: bankList, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpGet("TransactionApprovalHistory")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblCorporateApprovalHistory>> TransactionApprovalHistory(string transLogId)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);

				if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
				{
					if (_authType == AuthorizationType.Single_Signatory)
					{
						LogFormater<TransactionController>.Error(_logger, "TransactionApprovalHistory", "Sorry  this feature is not available for Single Signatory", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Sorry  this feature is not available for Single Signatory ");
					}
				}



				var Id = Encryption.DecryptGuid(transLogId);
				var tblTransactions = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryPendingTrandLogId(Id, (Guid)CorporateProfile.CorporateCustomerId);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));

			}
		}

		protected ValidationStatus ValidateWorkflowAccess(Guid? workflowId, decimal amount)
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

			if (workFlow.ApprovalLimit < amount)
			{
				return new ValidationStatus { Status = false, Message = "Approval limit of Workflow selected is less than transaction amount" };
			}


			//get workflow hierarchy
			//.WorkflowHierarchies.GetWorkflowHierarchiesByWorkflowID(tblWorkflow.Id.ToString())
			var WorkflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(workFlow.Id);
			if (WorkflowHierarchies.Count == 0)
			{
				return new ValidationStatus { Status = false, Message = "No workflow level found" };
			}
			if (WorkflowHierarchies.Count != workFlow.NoOfAuthorizers)
			{
				return new ValidationStatus { Status = false, Message = "Workflow Authorize is not valid " };
			}
			return new ValidationStatus { Status = true, Message = "Validation OK" };
		}
		private ValidationStatus ValidateTransactionApproval(TblCorporateProfile tblCorporateProfile, Guid approverId, int approvalLevel, int authLevel, decimal amount, out string errormsg)
		{
			errormsg = string.Empty;
			if (approvalLevel == authLevel && approverId == tblCorporateProfile.Id)
			{
				//if (item.AccountLimit < amount)
				//{
				//    errormsg = "Your set transaction approval limit has been exceeded";
				//    return false;
				//}

			}
			else
			{
				return new ValidationStatus { Status = false, Message = "Your authorization level could not be determined. Please contact your customer support team" };
			}
			return new ValidationStatus { Status = true, Message = "Your authorization level could not be determined. Please contact your customer support team" };
		}

		private async Task<IntraBankTransferResponse> PostIntraBankTransfer(TblPendingCreditLog pendingCreditLog, TblPendingTranLog pendingTranLog, DateTime date, string transactionReference, TblAuditTrail trail)
		{
			var transfer = new IntraBankPostDto
			{
				AccountToDebit = pendingTranLog.DebitAccountNumber,
				UserName = CorporateProfile.Username,
				Channel = "2",
				TransactionLocation = pendingTranLog.TransactionLocation,
				IntraTransferDetails = new List<IntraTransferDetail>{
					new IntraTransferDetail {
						TransactionReference = pendingCreditLog.TransactionReference,
						TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
						BeneficiaryAccountName = pendingCreditLog.CreditAccountName,
						BeneficiaryAccountNumber = pendingCreditLog.CreditAccountNumber,
						Amount = pendingCreditLog.CreditAmount,
						Narration = pendingCreditLog.Narration,
					}
				},
			};
			var transferResult = await _apiService.IntraBankTransfer(transfer);
			if (transferResult.ResponseCode != "00")
			{
				var tranxt = new TblTransaction
				{
					Id = Guid.NewGuid(),
					TranAmout = pendingCreditLog.CreditAmount,
					DestinationAcctName = pendingCreditLog.CreditAccountName,
					DestinationAcctNo = pendingCreditLog.CreditAccountNumber,
					DesctionationBank = "Parallex Bank",
					TranType = pendingTranLog.TransferType,
					TransactionStatus = nameof(TransactionStatus.Failed),
					Narration = pendingTranLog.Narration,
					SourceAccountName = pendingTranLog.DebitAccountName,
					SourceAccountNo = pendingTranLog.DebitAccountNumber,
					SourceBank = "Parallex Bank",
					CustAuthId = CorporateProfile.Id,
					Channel = "WEB",
					TransactionReference = pendingCreditLog.TransactionReference,
					SessionId = transferResult.TransactionReference,
					TranDate = date,
					CorporateCustomerId = CorporateProfile.CorporateCustomerId,
					BatchId = pendingTranLog.BatchId,
				};
				var audit = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Intra_Bank_Transfer).Replace("_", " "),
					Ipaddress = trail.Ipaddress,
					Macaddress = trail.Macaddress,
					HostName = trail.HostName,
					ClientStaffIpaddress = trail.ClientStaffIpaddress,
					NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
					PreviousFieldValue = "",
					TransactionId = pendingCreditLog.TransactionReference,
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Intrabank transfer",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(audit);
				UnitOfWork.TransactionRepo.Add(tranxt);
			}
			else
			{
				var tranxt = new TblTransaction
				{
					Id = Guid.NewGuid(),
					TranAmout = pendingCreditLog.CreditAmount,
					DestinationAcctName = pendingCreditLog.CreditAccountName,
					DestinationAcctNo = pendingCreditLog.CreditAccountNumber,
					DesctionationBank = "Parallex Bank",
					TranType = pendingTranLog.TransferType,
					TransactionStatus = nameof(TransactionStatus.Successful),
					Narration = pendingTranLog.Narration,
					SourceAccountName = pendingTranLog.DebitAccountName,
					SourceAccountNo = pendingTranLog.DebitAccountNumber,
					SourceBank = "Parallex Bank",
					CustAuthId = CorporateProfile.Id,
					Channel = "WEB",
					TransactionReference = pendingCreditLog.TransactionReference,
					SessionId = transferResult.TransactionReference,
					TranDate = date,
					CorporateCustomerId = CorporateProfile.CorporateCustomerId,
					BatchId = pendingTranLog.BatchId,
				};
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Intra_Bank_Transfer).Replace("_", " "),
					Ipaddress = trail.Ipaddress,
					Macaddress = trail.Macaddress,
					HostName = trail.HostName,
					ClientStaffIpaddress = trail.ClientStaffIpaddress,
					NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
					PreviousFieldValue = "",
					TransactionId = pendingCreditLog.TransactionReference,
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Intrabank transfer",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.TransactionRepo.Add(tranxt);
			}
			UnitOfWork.Complete();
			return transferResult;
		}
		protected async Task<IntraBankTransferResponse> PostInterBankTransfer(TblPendingCreditLog pendingCreditLog, TblPendingTranLog pendingTranLog, DateTime date, string transactionReference, TblAuditTrail trail)
		{
			var transfer = new InterBankPostDto
			{
				accountToDebit = pendingTranLog.DebitAccountNumber,
				userName = CorporateProfile.Username,
				channel = "2",
				transactionLocation = pendingTranLog.TransactionLocation,
				interTransferDetails = new List<InterTransferDetail>{
					new InterTransferDetail {
						transactionReference = pendingCreditLog.TransactionReference,
						beneficiaryAccountName = pendingCreditLog.CreditAccountName,
						beneficiaryAccountNumber = pendingCreditLog.CreditAccountNumber,
						transactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
						amount = pendingCreditLog.CreditAmount,
						customerRemark = pendingCreditLog.Narration,
						beneficiaryBVN = pendingCreditLog.BankVerificationNo,
						beneficiaryKYC = pendingCreditLog.KycLevel,
						beneficiaryBankCode = pendingCreditLog.CreditBankCode,
						beneficiaryBankName = pendingCreditLog.CreditBankName,
						nameEnquirySessionID = pendingCreditLog.NameEnquiryRef
					}
				},
			};
			var transferResult = await _apiService.InterBankTransfer(transfer);
			if (transferResult.ResponseCode != "00")
			{
				var tranxt = new TblTransaction
				{
					Id = Guid.NewGuid(),
					TranAmout = pendingCreditLog.CreditAmount,
					DestinationAcctName = pendingCreditLog.CreditAccountName,
					DestinationAcctNo = pendingCreditLog.CreditAccountNumber,
					DesctionationBank = pendingCreditLog.CreditBankName,
					TranType = pendingTranLog.TransferType,
					TransactionStatus = nameof(TransactionStatus.Failed),
					Narration = pendingTranLog.Narration,
					SourceAccountName = pendingTranLog.DebitAccountName,
					SourceAccountNo = pendingTranLog.DebitAccountNumber,
					SourceBank = "Parallex Bank",
					CustAuthId = CorporateProfile.Id,
					Channel = "WEB",
					TransactionReference = pendingCreditLog.TransactionReference,
					SessionId = transferResult.TransactionReference,
					TranDate = date,
					CorporateCustomerId = CorporateProfile.CorporateCustomerId,
					BatchId = pendingTranLog.BatchId,
				};
				var audit = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Intra_Bank_Transfer).Replace("_", " "),
					Ipaddress = trail.Ipaddress,
					Macaddress = trail.Macaddress,
					HostName = trail.HostName,
					ClientStaffIpaddress = trail.ClientStaffIpaddress,
					NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
					PreviousFieldValue = "",
					TransactionId = pendingCreditLog.TransactionReference,
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Interbank transfer",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(audit);
				UnitOfWork.TransactionRepo.Add(tranxt);
			}
			else
			{
				var Tranxt = new TblTransaction
				{
					Id = Guid.NewGuid(),
					TranAmout = pendingCreditLog.CreditAmount,
					DestinationAcctName = pendingCreditLog.CreditAccountName,
					DestinationAcctNo = pendingCreditLog.CreditAccountNumber,
					DesctionationBank = pendingCreditLog.CreditBankName,
					TranType = pendingTranLog.TransferType,
					TransactionStatus = nameof(TransactionStatus.Successful),
					Narration = pendingTranLog.Narration,
					SourceAccountName = pendingTranLog.DebitAccountName,
					SourceAccountNo = pendingTranLog.DebitAccountNumber,
					SourceBank = "Parallex Bank",
					CustAuthId = CorporateProfile.Id,
					Channel = "WEB",
					TransactionReference = pendingCreditLog.TransactionReference,
					SessionId = transferResult.TransactionReference,
					TranDate = date,
					CorporateCustomerId = CorporateProfile.CorporateCustomerId,
					BatchId = pendingTranLog.BatchId,
				};
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Inter_Bank_Transfer).Replace("_", " "),
					Ipaddress = trail.Ipaddress,
					Macaddress = trail.Macaddress,
					HostName = trail.HostName,
					ClientStaffIpaddress = trail.ClientStaffIpaddress,
					NewFieldValue = "Customer " + CorporateProfile.FullName + " transfer " + transferResult.TransactionAmount + "to " + transferResult.AccountCredited,
					PreviousFieldValue = "",
					TransactionId = pendingCreditLog.TransactionReference,
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Interbank transfer",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.TransactionRepo.Add(Tranxt);
			}
			UnitOfWork.Complete();
			return transferResult;
		}
		private bool DailyLimitExceeded(TblCorporateCustomer tblCorporateCustomer, decimal amount, out string errorMsg)
		{
			errorMsg = string.Empty;
			//check cummulatve daily transfer limit
			var dairlyLimit = _unitOfWork.TransactionHistoryRepo.GetTransactionHistory(tblCorporateCustomer.Id, DateTime.Now.Date);
			if (dairlyLimit != null)
			{
				decimal amtTransferable = (decimal)tblCorporateCustomer.SingleTransDailyLimit - (decimal)dairlyLimit.SingleTransTotalAmount;
				if (amtTransferable < amount)
				{
					if (amtTransferable <= 0)
					{
						var message = "You have exceeded your daily Single transaction limit Which is";
						errorMsg = $"{message}{Helper.formatCurrency(tblCorporateCustomer.SingleTransDailyLimit)}";
						return true;
					}
					var msg = $"Transaction amount {Helper.formatCurrency(amount)} has exceeded the maximum daily transaction limit";
					errorMsg = $"{msg}{Helper.formatCurrency(tblCorporateCustomer.SingleTransDailyLimit)} for your organisation. You can only transfer {Helper.formatCurrency(amtTransferable)} for the rest of the day";
					return true;
				}

			}
			return false;
		}

		[HttpGet("DailyTranferLimitInfo")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public ActionResult<AccountLimitModel> DailyTransferLimitInfo()
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

				var tblCorporateCustomer = _unitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
				if (CorporateProfile != null)
				{

					if (tblCorporateCustomer == null)
					{
						return BadRequest("Invalid corporate customer id");
					}

				}
				else
				{
					return BadRequest("UnAuthorized Access");
				}

				//get daily account limit
				var dailyLimit = _unitOfWork.TransactionHistoryRepo.GetTransactionHistory((Guid)CorporateProfile.CorporateCustomerId, DateTime.Now);

				var dto = new AccountLimitModel
				{
					BulkTransAmountLeft = dailyLimit?.BulkTransAmountLeft ?? tblCorporateCustomer.BulkTransDailyLimit,
					BulkTransTotalAmount = dailyLimit?.BulkTransTotalAmount ?? 0,
					SingleTransAmountLeft = dailyLimit?.SingleTransAmountLeft ?? tblCorporateCustomer.SingleTransDailyLimit,
					SingleTransTotalAmount = dailyLimit?.SingleTransTotalAmount ?? 0,
					BulkTransTotalCount = dailyLimit?.BulkTransTotalCount ?? 0,
					CorporateCustomerId = tblCorporateCustomer.Id,
					CorporateCustomerMaxLimit = tblCorporateCustomer.MaxAccountLimit ?? 0,
					CurrentUserMaxLimit = CorporateProfile.ApprovalLimit ?? 0,
					CustomerId = tblCorporateCustomer.CustomerId,
					Date = DateTime.Now,
					SingleTransTotalCount = dailyLimit?.SingleTransTotalCount ?? 0,
					BulkTransMaxDailyLimit = tblCorporateCustomer.BulkTransDailyLimit ?? 0,
					SingleTransMaxDailyLimit = tblCorporateCustomer.SingleTransDailyLimit ?? 0,
					Currency = "NGN"
				};

				return Ok(dto);
			}
			catch (Exception ex)
			{

				//_logger.LogError($"{controller}: Daily Transfer Limit Info: {ex.InnerException.Message}");
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		private TransactionRecipt GetTransactionRecipt(TblPendingTranLog tranLog, TblPendingCreditLog creditLog)
		{
			var transactionStatus = creditLog.CreditStatus == 1 ? "Successful" : creditLog.CreditStatus == 0 ? "Pending" : "Failed";
			var recipt = new TransactionRecipt(
				tranLog.DebitAmount,
				tranLog.DebitAccountNumber,
				tranLog.DebitAccountName,
				"Parallex Bank",
				creditLog.CreditDate,
				tranLog.TransferType,
				tranLog.Narration,
				creditLog.CreditAccountNumber,
				creditLog.CreditAccountName,
				creditLog.CreditBankName,
				creditLog.TransactionReference,
				transactionStatus);
			return recipt;
		}
		private async Task<List<RelatedCustomerAccountDetail>> GetRelatedAccountNumber(List<TblCorporateAccountAggregation>? aggregateAccount)
		{
			var accountList = new List<RelatedCustomerAccountDetail>();
			foreach (var account in aggregateAccount)
			{
				var result = await _apiService.RelatedCustomerAccountDetails(account.CustomerId);
				if (result.RespondCode == "00")
				{
					var addedRelatedAccount = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAggregateId(account.Id);
					if (addedRelatedAccount.Any())
					{
						foreach (var returnAccount in addedRelatedAccount)
						{
							var resultAccount = result?.Records?.FirstOrDefault(ctx => ctx.AccountNumber == returnAccount.AccountNumber);
							if (resultAccount != null)
							{
								accountList.Add(resultAccount);
							}
						}
					}
				}
			}
			return accountList;
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
						LogFormater<TransactionController>.Error(_logger, "Validate Aggregated RelatedAccount", accountNumber, JsonConvert.SerializeObject(validateAccount));
						validateStatus.Message = "Source Account Verification Failed";
						validateStatus.Status = false;
						return validateStatus;
					}

					var getRelatedAccountCustomerId = isAggregationEnable.FirstOrDefault(ctx => ctx.DefaultAccountNumber == validateAccount.AccountNumber);
					if (getRelatedAccountCustomerId == null)
					{
						LogFormater<TransactionController>.Error(_logger, "VValidate Aggregated RelatedAccount", accountNumber, JsonConvert.SerializeObject(validateAccount));
						validateStatus.Message = "The Selected Account cannot be verified";
						validateStatus.Status = false;
						return validateStatus;
					}
					var result = await _apiService.RelatedCustomerAccountDetails(getRelatedAccountCustomerId.CustomerId);
					if (!AccountValidation.RelatedAccount(result, accountNumber, out string validationtError))
					{
						LogFormater<TransactionController>.Error(_logger, "ValidateRelatedAccount", validationtError, JsonConvert.SerializeObject(getRelatedAccountCustomerId.CustomerId));
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
					LogFormater<TransactionController>.Error(_logger, "ValidateRelatedAccount", aggregateErrorMessage, JsonConvert.SerializeObject(corporateCustomer?.CustomerId));
					validateStatus.Message = aggregateErrorMessage;
					validateStatus.Status = false;
					return validateStatus;
				}
			}

			var corporateAccount = await _apiService.RelatedCustomerAccountDetails(corporateCustomer?.CustomerId);
			if (!AccountValidation.RelatedAccount(corporateAccount, accountNumber, out string aggregatedErrorMessage))
			{
				LogFormater<TransactionController>.Error(_logger, "ValidateRelatedAccount", aggregatedErrorMessage, JsonConvert.SerializeObject(corporateCustomer?.CustomerId));
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
