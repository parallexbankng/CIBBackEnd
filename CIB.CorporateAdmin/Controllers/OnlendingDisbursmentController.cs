
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Exceptions;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using CIB.Core.Modules.OnLending.Enums;
using CIB.Core.Modules.OnLending.TransferLog.Dto;
using CIB.Core.Services._2FA;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Services.Notification;
using CIB.Core.Services.OnlendingApi;
using CIB.Core.Services.OnlendingApi.Dto;
using CIB.Core.Templates;
using CIB.Core.Utils;
using CIB.CorporateAdmin.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class OnlendingDisbursmentController : BaseAPIController
	{
		private readonly ILogger<OnlendingDisbursmentController> _logger;
		private readonly IApiService _apiService;
		private readonly IOnlendingServiceApi _onlendingApi;
		private readonly IEmailService _emailService;
		private readonly IFileService _fileService;
		private readonly IConfiguration _config;
		private readonly IToken2faService _2FaService;
		private readonly INotificationService _notify;
		public OnlendingDisbursmentController(
				INotificationService notify,
				ILogger<OnlendingDisbursmentController> logger,
				IApiService apiService,
				IUnitOfWork unitOfWork,
				IMapper mapper,
				IHttpContextAccessor accessor,
				IEmailService emailService,
				IFileService fileService,
				IToken2faService token2FaService,
				IOnlendingServiceApi onlendingApi,
				IConfiguration config, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
		{
			_apiService = apiService;
			_emailService = emailService;
			_fileService = fileService;
			_config = config;
			_2FaService = token2FaService;
			_logger = logger;
			_notify = notify;
			_onlendingApi = onlendingApi;
		}



		[HttpPost("InitiateDisbursment")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<ResponseDTO<VerifyResponse>>> InitiateDisbursment([FromBody] InitiateDisbursmentRequest model)
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

				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
				if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
				{
					return BadRequest(corporateCustomerErrorMessage);
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
				{
					if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
					{
						return BadRequest(authorizeErrorMessage);
					}
				}
				_ = Enum.TryParse(corporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);

				var beneficiary = JsonConvert.DeserializeObject<List<BeneficiaryId>>(Encryption.DecryptStrings(model.Beneficiaries));
				var payload = new InitiateDisbursment
				{
					BatchId = Encryption.DecryptGuid(model.BatchId),
					Beneficiaries = beneficiary,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};


				if (!payload.Beneficiaries.Any())
				{
					return BadRequest($"No Beneficiary has been selected");
				}

				var getBatchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingByBatchId(payload.BatchId);
				if (getBatchInfo is null) return BadRequest($"Source account number could not be verified ");


				if (_auth != AuthorizationType.Single_Signatory)
				{
					if (getBatchInfo.WorkflowId == null)
					{
						LogFormater<OnlendingDisbursmentController>.Error(_logger, "InitiateDisbursment", "Workflow is required", JsonConvert.SerializeObject(getBatchInfo.WorkflowId), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Workflow is required");
					}
				}

				var senderInfo = await _apiService.CustomerNameInquiry(getBatchInfo.DebitAccountNumber);
				if (!AccountValidation.SourceAccount(senderInfo, out string sourceAccountErrorMessage))
				{
					return BadRequest(sourceAccountErrorMessage);
				}

				//var corporateAccount = await _apiService.RelatedCustomerAccountDetails(corporateCustomer.CustomerId);
				//if (!AccountValidation.RelatedAccount(corporateAccount, getBatchInfo.DebitAccountNumber, out string relatedAccountErrorMessage))
				//{
				//	return BadRequest(relatedAccountErrorMessage);
				//}

				var processValidBatchItems = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingValidBatchByBatchId(payload.BatchId);
				if (!processValidBatchItems.Any())
				{
					return BadRequest($"No beneficiaries is selected on this batch ");
				}

				var interestCalculationResult = await this.CalculateIntrest(processValidBatchItems);
				if (!interestCalculationResult.Any())
				{
					return BadRequest($"Error Calculate Interest please try again");
				}

				if (senderInfo.AvailableBalance < interestCalculationResult.Sum(ctx => ctx.Interest))
				{
					return BadRequest($"Source account cannot cover the interest payment, kindly fund the account to and continue disbursement.");
				}

				var beneficiaries = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryByTranLogId(getBatchInfo.Id);
				if (!beneficiaries.Any())
				{
					return BadRequest($"Batch Beneficiaries cannot be empty");
				}

				if (_auth == AuthorizationType.Single_Signatory)
				{
					var disbursmentBeneficiaryList = PrepareBeneficiaryList(processValidBatchItems, payload.Beneficiaries, getBatchInfo);
					if (!disbursmentBeneficiaryList.Any())
					{
						return BadRequest($"Disbursment cannot be completed and the moment please try again later ");
					}

					var failedDisbursmentList = new List<TblOnlendingCreditLog>();
					var processDisbursmentList = new List<TblOnlendingCreditLog>();

					foreach (var beneficary in disbursmentBeneficiaryList)
					{
						var processItem = beneficiaries.FirstOrDefault(ctx => ctx.AccountNumber == beneficary.beneficiaryAccountNumber);
						if (beneficary.beneficiaryAccountNumber == processItem?.AccountNumber)
						{
							var disbustResult = await _onlendingApi.InitiateBeneficiaryDisbursment(beneficary);
							processItem.DateCredited = DateTime.Now;
							processItem.DisbursmentResponseCode = disbustResult.ResponseCode;
							processItem.DisbursmentResponseMessage = disbustResult.ResponseMessage;
							if (disbustResult.ResponseCode != "00")
							{
								processItem.CreditStatus = (int)OnlendingStatus.Failed;
								failedDisbursmentList.Add(processItem);
							}
							else
							{
								processItem.CreditStatus = (int)OnlendingStatus.Active;
								processDisbursmentList.Add(processItem);
							}
							_unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLog(processItem);
							_unitOfWork.Complete();
						}
					}
					_unitOfWork.Complete();
					return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful", ProcessDisbursment = processDisbursmentList.Count(), FailedDisburment = processDisbursmentList.Count() });
				}


				var approvelList = _unitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryPendingTrandLogId(getBatchInfo.Id, corporateCustomer.Id);
				if (!approvelList.Any())
				{
					return BadRequest($"Work flow approver");
				}

				foreach (var ben in payload.Beneficiaries)
				{
					var item = beneficiaries.FirstOrDefault(ctx => ctx.Id == ben.Id);
					if (item != null)
					{
						item.Status = (int)OnlendingStatus.Process;
						_unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLog(item);
					}
				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.initiate_Onlending_Transaction).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Approve transfer of " + getBatchInfo.TotalValidAmount + " from " + getBatchInfo.DebitAccountNumber,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Corporate User Initiated Bulk transfer",
					TimeStamp = DateTime.Now
				};

				_unitOfWork.AuditTrialRepo.Add(auditTrail);
				_unitOfWork.Complete();
				var firstApproval = approvelList.First(ctx => ctx.ApprovalLevel == 1);
				var corporateUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)firstApproval?.UserId);
				var initiatorName = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)getBatchInfo.InitiatorId);
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(corporateUser.Email, initiatorName.FullName, string.Format("{0:0.00}", getBatchInfo.TotalValidAmount))));
				return Ok(new { Responsecode = "00", ResponseDescription = "Disbursment has been forwarded for approval" });
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpGet("PendingDisbursment")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<List<TblNipbulkTransferLog>>> PendingDisbursment()
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
						LogFormater<OnlendingDisbursmentController>.Error(_logger, "PendingBulkTransactionLogs", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
						return BadRequest("Authorization type could not be determined!!!");
					}
				}
				var tblTransactions = await _unitOfWork.OnlendingTransferLogRepo.GetAllOnlendingBatches(tblCorporateCustomer.Id);
				return tblTransactions?.Count() > 0 ? Ok(tblTransactions.OrderByDescending(x => x.DateInitiated)) : Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("AuthorizedDisbursment")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> AuthorizedDisbursment(string corporateCustomerId)
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
						LogFormater<OnlendingDisbursmentController>.Error(_logger, "AuthorizedBulkTransactionLogs", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
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

		[HttpGet("DeclineDisbursment")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblPendingTranLog>> DeclineDisbursment()
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
						LogFormater<OnlendingDisbursmentController>.Error(_logger, "DeclineTransactions", "Authorization type could not be determined!!!", JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId));
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

		[HttpGet("OnlendingCreditLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> OnlendingCreditLogs(string tranLogId)
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

				if (string.IsNullOrEmpty(tranLogId))
				{
					return BadRequest("Bulk file Id is required");
				}

				var id = Encryption.DecryptGuid(tranLogId);
				var tblTransactions = _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryByTranLogId(id);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("FailedOnlendingCreditLogs")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblNipbulkTransferLog>> FailedBulkCreditLogs(string tranLogId)
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

				if (string.IsNullOrEmpty(tranLogId))
				{
					return BadRequest("Bulk file Id is required");
				}

				var id = Encryption.DecryptGuid(tranLogId);
				var tblTransactions = _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryCreditLogStatus(id, 2);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("OnlendingApprovalHistory")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblCorporateBulkApprovalHistory>> BulkTransactionApprovalHistory(string tranLogId)
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

				if (string.IsNullOrEmpty(tranLogId))
				{
					return BadRequest("Bulk file Id is required");
				}

				var id = Encryption.DecryptGuid(tranLogId);
				var tblTransactions = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateBulkAuthorizationHistories(id);
				return Ok(tblTransactions);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}


		[HttpPost("ApproveRequest")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<ResponseDTO<VerifyResponse>>> ApproveDisbursment([FromBody] InitiateDisbursmentRequest model)
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

				var payload = new ApprovedInitiateDisbursment
				{
					BatchId = Encryption.DecryptGuid(model.BatchId),
					Otp = Encryption.DecryptStrings(model.Otp),
					Comment = Encryption.DecryptStrings(model.Commemt),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
				};

				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(id: (Guid)CorporateProfile?.CorporateCustomerId);
				var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
				// var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
				// if(validOTP.ResponseCode != "00"){
				//   LogFormater<BulkTransactionController>.Error(_logger,"DeclineTransaction",$"2FA API ERROR:{validOTP.ResponseMessage}",JsonConvert.SerializeObject(userName),JsonConvert.SerializeObject(corporateCustomer.CustomerId));
				//   return BadRequest(validOTP.ResponseMessage);
				// }

				if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
				{
					return BadRequest(corporateCustomerErrorMessage);
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
				{
					if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
					{
						return BadRequest(authorizeErrorMessage);
					}
				}

				var pendingTranLog = await _unitOfWork.OnlendingTransferLogRepo.GetPendingOnlendingTransferLog(payload.BatchId);
				if (pendingTranLog == null)
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "ApproveRequest", "Invalid transaction log id", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Invalid transaction log id");
				}

				if (pendingTranLog.Status != 0)
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "ApproveRequest", "Transaction is no longer pending approval", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Transaction is no longer pending approval");
				}

				var corporateApprovalHistory = UnitOfWork.CorporateApprovalHistoryRepo.GetCorporateAuthorizationHistoryByAuthId(CorporateProfile.Id, pendingTranLog.Id);
				if (corporateApprovalHistory == null)
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "ApproveRequest", "Corporate approval history could not be retrieved", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Corporate approval history could not be retrieved");
				}

				if (corporateApprovalHistory.Status == nameof(AuthorizationStatus.Approved))
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "ApproveRequest", "This transaction has already been approved by you.", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("This transaction has already been approved by you.");
				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Onlending_Disburment).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Approve Onlending transfer of " + pendingTranLog.TotalValidAmount + " from " + pendingTranLog.DebitAccountNumber,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = "Corporate User Initiated Onlending transfer",
					TimeStamp = DateTime.Now
				};

				var date = DateTime.Now;
				if (pendingTranLog.ApprovalCount == pendingTranLog.ApprovalStage)
				{
					var tranDate = DateTime.Now;
					var bulkSuspenseVatFee = new List<TblNipbulkCreditLog>();
					var tblPendingCreditLog = _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryCreditLogStatus(pendingTranLog.Id, 0);
					if (tblPendingCreditLog == null)
					{
						LogFormater<OnlendingDisbursmentController>.Error(_logger, "ApproveBulkTransfer", "Credit log info could not be retrieved", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Credit log info could not be retrieved");
					}

					if (pendingTranLog.TransferType != nameof(TransactionType.OnLending))
					{
						LogFormater<OnlendingDisbursmentController>.Error(_logger, "ApproveBulkTransfer", "Bulk Transaction only is allow", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
						return BadRequest("Bulk Transaction only is allow");
					}

					var senderInfo = await _apiService.CustomerNameInquiry(pendingTranLog.DebitAccountNumber);
					if (!AccountValidation.SourceAccount(senderInfo, out string sourceAccountErrorMessage))
					{
						return BadRequest(sourceAccountErrorMessage);
					}

					var corporateAccount = await _apiService.RelatedCustomerAccountDetails(corporateCustomer.CustomerId);
					if (!AccountValidation.RelatedAccount(corporateAccount, pendingTranLog.DebitAccountNumber, out string relatedAccountErrorMessage))
					{
						return BadRequest(relatedAccountErrorMessage);
					}

					var processValidBatchItems = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingValidBatchByBatchId(payload.BatchId);
					if (!processValidBatchItems.Any())
					{
						return BadRequest($"No beneficiaries is selected on this batch ");
					}

					var interestCalculationResult = await this.CalculateIntrest(processValidBatchItems);
					if (!interestCalculationResult.Any())
					{
						return BadRequest($"Error Calculate Interest please try again");
					}

					if (senderInfo.AvailableBalance < interestCalculationResult.Sum(ctx => ctx.Interest))
					{
						return BadRequest($"Source account cannot cover the interest payment, kindly fund the account to and continue disbursement.");
					}

					var beneficiaryIds = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryIdsByTranLogId(pendingTranLog.Id);
					if (!beneficiaryIds.Any())
					{
						return BadRequest($"Batch Beneficiaries cannot be empty");
					}

					var disbursmentBeneficiaryList = PrepareBeneficiaryList(processValidBatchItems, beneficiaryIds.ToList(), pendingTranLog);
					if (!disbursmentBeneficiaryList.Any())
					{
						return BadRequest($"Disbursment cannot be completed and the moment please try again later ");
					}

					var failedDisbursmentList = new List<TblOnlendingCreditLog>();
					var processDisbursmentList = new List<TblOnlendingCreditLog>();

					var beneficiaries = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryByTranLogId(pendingTranLog.Id);

					foreach (var beneficary in disbursmentBeneficiaryList)
					{
						var processItem = beneficiaries.FirstOrDefault(ctx => ctx.AccountNumber == beneficary.beneficiaryAccountNumber);
						if (beneficary.beneficiaryAccountNumber == processItem?.AccountNumber)
						{
							var disbustResult = await _onlendingApi.InitiateBeneficiaryDisbursment(beneficary);
							processItem.DateCredited = DateTime.Now;
							processItem.DisbursmentResponseCode = disbustResult.ResponseCode;
							processItem.DisbursmentResponseMessage = disbustResult.ResponseMessage;
							if (disbustResult.ResponseCode != "00")
							{
								processItem.CreditStatus = 2;
								failedDisbursmentList.Add(processItem);
							}
							else
							{
								processItem.CreditStatus = 1;
								processDisbursmentList.Add(processItem);
							}
							_unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLog(processItem);
							_unitOfWork.Complete();
						}
					}
					_unitOfWork.Complete();
					return Ok(new { Responsecode = "00", ResponseDescription = "Disburment Successful", ProcessDisbursment = processDisbursmentList.Count(), FailedDisburment = processDisbursmentList.Count() });
				}

				pendingTranLog.ApprovalStage += 1;
				corporateApprovalHistory.Status = nameof(AuthorizationStatus.Approved);
				corporateApprovalHistory.ToApproved = 0;
				corporateApprovalHistory.ApprovalDate = date;
				corporateApprovalHistory.Comment = payload.Comment;
				corporateApprovalHistory.UserId = CorporateProfile.Id;
				_unitOfWork.AuditTrialRepo.Add(auditTrail);
				_unitOfWork.OnlendingTransferLogRepo.UpdateOnlendingTransferLog(pendingTranLog);
				_unitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(corporateApprovalHistory);
				_unitOfWork.Complete();

				var newTransApprover = UnitOfWork.CorporateApprovalHistoryRepo.GetNextOnlendingApproval(pendingTranLog);
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
						Amount = $"{pendingTranLog.TotalValidAmount:0.00}"
					};
					_notify.NotifyCorporateTransfer(initiatorInfo, approvalInfo, dto, payload.Comment);
				}
				return Ok(new { Responsecode = "00", ResponseDescription = "Disburment Approve Successfully" });
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("DeclineRequest")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<ResponseDTO<VerifyResponse>>> DeclineDisbursment([FromBody] ApprovedInitiateDisbursmentRequest model)
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

				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
				if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
				{
					return BadRequest(corporateCustomerErrorMessage);
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
				{
					if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
					{
						return BadRequest(authorizeErrorMessage);
					}
				}

				var payload = new ApprovedInitiateDisbursment
				{
					Comment = Encryption.DecryptStrings(model.Comment),
					Otp = Encryption.DecryptStrings(model.Otp),
					BatchId = Encryption.DecryptGuid(model.BatchId),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
				};

				var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
				// var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
				// if(validOTP.ResponseCode != "00"){
				//   LogFormater<BulkTransactionController>.Error(_logger,"DeclineTransaction",$"2FA API ERROR:{validOTP.ResponseMessage}",JsonConvert.SerializeObject(userName),JsonConvert.SerializeObject(corporateCustomer.CustomerId));
				//   return BadRequest(validOTP.ResponseMessage);
				// }

				var pendingTranLog = await _unitOfWork.OnlendingTransferLogRepo.GetPendingOnlendingTransferLog(payload.BatchId);
				if (pendingTranLog == null)
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "DeclineTransaction", "Invalid transaction log id", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Invalid transaction log id");
				}

				if (pendingTranLog.Status != (int)ProfileStatus.Pending)
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "DeclineTransaction", "Transaction is no longer pending approval", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Transaction is no longer pending approval");
				}
				var tblPendingCreditLog = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingBeneficiaryByTranLogId(pendingTranLog.Id);

				if (tblPendingCreditLog == null)
				{
					LogFormater<OnlendingDisbursmentController>.Error(_logger, "DeclineTransaction", "Credit log info could not be retrieved", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(corporateCustomer.CustomerId));
					return BadRequest("Credit log info could not be retrieved");
				}

				var creditLogList = new List<TblOnlendingCreditLog>();
				foreach (var item in tblPendingCreditLog)
				{
					item.CreditStatus = (int)ProfileStatus.Declined;
					item.DateCredited = DateTime.Now;
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
					NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Decline transfer of " + pendingTranLog.TotalValidAmount + "from " + pendingTranLog.DebitAccountNumber + " To Suspense Account " + parallexSuspenseAccount,
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = CorporateProfile.Username,
					Description = $"Corporate Authorizer Decline Onlending transfer reason been: {payload.Comment}",
					TimeStamp = DateTime.Now
				};
				//update tables
				_unitOfWork.AuditTrialRepo.Add(failedauditTrail);
				_unitOfWork.OnlendingTransferLogRepo.UpdateOnlendingTransferLog(pendingTranLog);
				_unitOfWork.CorporateApprovalHistoryRepo.UpdateCorporateApprovalHistory(corporateApprovalHistory);
				_unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLogList(creditLogList);
				_unitOfWork.Complete();
				var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)pendingTranLog?.InitiatorId);
				var dto = new EmailNotification
				{
					Action = nameof(AuthorizationStatus.Decline),
					Amount = string.Format("{0:0.00}", pendingTranLog.TotalValidAmount)
				};
				_notify.NotifyCorporateTransfer(initiatorProfile, null, dto, payload.Comment);
				return Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		private async Task<List<IntrestCalculationResponse>> CalculateIntrest(IEnumerable<BeneficiaryDto> processValidBatchItems)
		{
			var totalInterest = new List<IntrestCalculationResponse>();
			foreach (var item in processValidBatchItems)
			{
				var getDurationInDays = DateTime.Parse(item?.RepaymentDate).Date - DateTime.Now;
				var calculateIntrest = new OnlendingGetInterestRequest
				{
					AccountNumber = item?.AccountNumber,
					Amount = item?.FundAmount ?? 0,
					DurationIndays = getDurationInDays.Days
				};
				var result = await _onlendingApi.CalculateIntrest(calculateIntrest);
				if (result.ResponseCode != "00")
				{

				}
				else
				{
					var response = new IntrestCalculationResponse
					{
						AccountNumber = result?.ResponseData?.AccountNumber,
						Interest = result?.ResponseData?.Interest,
					};
					totalInterest.Add(response);
				}
			}
			return totalInterest;
		}

		private static List<OnlendingInitiateBeneficiaryDisburstment> PrepareBeneficiaryList(IEnumerable<BeneficiaryDto> processValidBatchItems, List<BeneficiaryId> beneficiaries, TblOnlendingTransferLog? getBatchInfo)
		{
			var qualifyBeneficiaries = new List<OnlendingInitiateBeneficiaryDisburstment>();
			foreach (var item in beneficiaries)
			{
				var finedItme = processValidBatchItems.FirstOrDefault(ctz => ctz.Id == item.Id);
				if (finedItme != null)
				{
					var disburstToBeneficiary = new OnlendingInitiateBeneficiaryDisburstment
					{
						RequestedAmount = finedItme?.FundAmount,
						ApprovedAmount = finedItme?.FundAmount,
						beneficiaryAccountNumber = finedItme?.AccountNumber,
						merchantAccountNumber = getBatchInfo?.DebitAccountNumber,
						MerchantOperatingAccountNumber = getBatchInfo.OperatingAccountNumber,
						StartDate = DateTime.Now,
						EndDate = DateTime.Parse(finedItme?.RepaymentDate)
					};
					qualifyBeneficiaries.Add(disburstToBeneficiary);
				}
			}

			return qualifyBeneficiaries;

		}

	}
}

