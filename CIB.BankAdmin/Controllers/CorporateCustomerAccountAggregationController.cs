using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using CIB.Core.Modules.AccountAggregation.Aggregations.Dto;
using CIB.Core.Modules.AccountAggregation.Aggregations.Validation;
using CIB.Core.Modules.AccountAggregationTemp.Aggregations.Dto;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.BankAdmin.Controllers;

[ApiController]
[Route("api/BankAdmin/v1/[controller]")]
public class CorporateCustomerAccountAggregationController : BaseAPIController
{
	private readonly IApiService _apiService;
	private readonly IEmailService _emailService;
	private readonly ILogger<CorporateCustomerController> _logger;
	private readonly IConfiguration _config;
	protected readonly INotificationService notify;
	public CorporateCustomerAccountAggregationController(INotificationService notify, IConfiguration config, IEmailService emailService, ILogger<CorporateCustomerController> _logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IApiService apiService, IAuthenticationService authService) : base(mapper, unitOfWork, accessor, authService)
	{
		this._apiService = apiService;
		this._logger = _logger;
		this._emailService = emailService;
		this._config = config;
		this.notify = notify;
	}

	[HttpPost("CorporateCustomerRelatedAccountInquiry")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<ResponseDTO<CorporateCustomerAccountInquireResponse>>> CorporateCustomerRelatedAccountInquiry(GenericRequestDto model)
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

			if (string.IsNullOrEmpty(model.Data))
			{
				return BadRequest("Invalid Request");
			}
			// //call name inquiry API
			var requestData = JsonConvert.DeserializeObject<AccountEnquire>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}
			var corporateCustomerInfo = _unitOfWork.CorporateCustomerRepo.GetByIdAsync(requestData.CorporateCustomerId);
			if (corporateCustomerInfo == null)
			{
				return BadRequest("Invalid Corporate customer Id");
			}
			var accountInfo = await _apiService.GetCustomerDetailByAccountNumber(requestData.AccountNumber.Trim());
			if (accountInfo.ResponseCode != "00")
			{
				return BadRequest(accountInfo.ResponseDescription);
			}
			var getRelatedAccounts = await _apiService.RelatedCustomerAccountDetails(accountInfo.CustomerId);
			if (getRelatedAccounts.RespondCode != "00")
			{
				return BadRequest(getRelatedAccounts.RespondMessage);
			}

			var acctInf = new CorporateCustomerAccountInquireResponse();
			var listAccount = new List<AggregatedAccountResponseDto>();
			foreach (var account in getRelatedAccounts.Records)
			{
				if (account.AccountNumber != null && account.AccountNumber != "")
				{
					var item = new AggregatedAccountResponseDto
					{
						AccountName = account.AccountName,
						AccountNumber = account.AccountNumber,
					};
					listAccount.Add(item);
				}
			}
			acctInf.CorporateCustomerId = corporateCustomerInfo.Id;
			acctInf.DefaultAccountName = accountInfo.AccountName;
			acctInf.DefaultAccountNumber = accountInfo.AccountNumber;
			acctInf.CustomerId = accountInfo.CustomerId;
			acctInf.RelatedAccounts = listAccount;
			return Ok(new ResponseDTO<CorporateCustomerAccountInquireResponse>(_data: acctInf, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpGet("GetRelatedAccounts")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<ResponseDTO<CorporateCustomerAccountInquireResponse>>> CorporateCustomerRelatedAccountInquiry(string customerId)
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

			var myCustomerId = Encryption.DecryptStrings(customerId);
			var getRelatedAccounts = await _apiService.RelatedCustomerAccountDetails(myCustomerId);
			if (getRelatedAccounts.RespondCode != "00")
			{
				return BadRequest(getRelatedAccounts.RespondMessage);
			}

			var acctInf = new CorporateCustomerAccountInquireResponse();
			var listAccount = new List<AggregatedAccountResponseDto>();
			foreach (var account in getRelatedAccounts.Records)
			{
				if (account.AccountNumber != null && account.AccountNumber != "")
				{
					var item = new AggregatedAccountResponseDto
					{
						AccountName = account.AccountName,
						AccountNumber = account.AccountNumber,
					};
					listAccount.Add(item);
				}
			}
			return Ok(new ListResponseDTO<AggregatedAccountResponseDto>(_data: listAccount, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpGet("GetCorporateCustomerAccountAggregations")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public ActionResult<ListResponseDTO<AggregationResponses>> GetCorporateCustomerAccountSetup(string corporateCustomerId)
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
				return BadRequest("Corporate Customer Account Setup Id is required");
			}
			var customerId = Encryption.DecryptGuid(corporateCustomerId);
			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(customerId);
			if (corporateCustomer == null)
			{
				return BadRequest("Invalid Corporate Customer Id");
			}

			var aggregations = UnitOfWork.CorporateAggregationRepo.AdminGetCorporateCustomerAggregations(customerId);
			if (!aggregations.Any())
			{
				return Ok(new ListResponseDTO<AggregationResponses>(_data: new(), success: true, _message: Message.Success));
			}
			var tempAggregationList = new List<AggregationResponses>();
			foreach (var account in aggregations)
			{
				var itme = new AggregationResponses
				{
					Id = account.Id,
					Sn = account.Sn,
					CorporateCustomerId = account.CorporateCustomerId,
					DefaultAccountNumber = account.DefaultAccountNumber,
					DefaultAccountName = account.DefaultAccountName,
					CustomerId = account.CustomerId,
					Status = account.Status,
					DateCreated = account.DateCreated,
					AccountNumbers = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAggregateId(account.Id),
				};
				tempAggregationList.Add(itme);
			}
			return Ok(new ListResponseDTO<AggregationResponses>(_data: tempAggregationList, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:GetCorporateCustomerAggregations {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpGet("GetCorporateCustomerAccountAggregation")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public ActionResult<ResponseDTO<AggregationResponses>> GetCorporateCustomerAccountAggregation(string corporateAggregateId)
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

			if (string.IsNullOrEmpty(corporateAggregateId))
			{
				return BadRequest("Corporate Customer Account Setup Id is required");
			}
			var customerId = Encryption.DecryptGuid(corporateAggregateId);
			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var aggregation = UnitOfWork.CorporateAggregationRepo.GetAggregationByAggregationCustomerId(customerId);
			if (aggregation == null)
			{
				return Ok(new ResponseDTO<AggregationResponses>(_data: new(), success: true, _message: Message.Success));
			}

			var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(aggregation.CorporateCustomerId);
			if (corporateCustomer == null)
			{
				return BadRequest("Invalid Corporate Customer Id");
			}


			return Ok(new ResponseDTO<AggregationResponses>(_data: aggregation, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:GetCorporateCustomerAggregations {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpGet("GetPendingCorporateCustomerAccountAggregations")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public ActionResult<ListResponseDTO<AggregationResponses>> GetPendingCorporateCustomerAccountAggregations(string corporateCustomerId)
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
				return BadRequest("Corporate Customer Account Setup Id is required");
			}

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}
			var corporateCustomer = Encryption.DecryptGuid(corporateCustomerId);
			var corporateCustomerData = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(corporateCustomer);
			if (corporateCustomerData == null)
			{
				return BadRequest("Invalid Corporate Customer Id");
			}

			var aggregations = UnitOfWork.TempCorporateAggregationRepo.GetPendingCorporateCustomerAggregations(corporateCustomer);
			if (!aggregations.Any())
			{
				return Ok(new ListResponseDTO<TempAggregationResponses>(_data: new(), success: true, _message: Message.Success));
			}
			var tempAggregationList = new List<TempAggregationResponses>();
			foreach (var account in aggregations)
			{
				var itme = new TempAggregationResponses
				{
					Id = account.Id,
					Sn = account.Sn,
					CorporateCustomerId = account.CorporateCustomerId,
					AccountAggregationId = account.AccountAggregationId,
					InitiatorId = account.InitiatorId,
					DefaultAccountNumber = account.DefaultAccountNumber,
					DefaultAccountName = account.DefaultAccountName,
					CustomerId = account.CustomerId,
					InitiatorUserName = account.InitiatorUserName,
					Status = account.Status,
					Action = account.Action,
					DateInitiated = account.DateInitiated
				};
				if (account.Action == nameof(AuditTrailAction.Update))
				{
					var myAccount = UnitOfWork.TempAggregatedAccountRepo.GetAllCorporateAggregationAccountByAggregationId(account.AccountAggregationId);
					itme.AccountNumbers = myAccount;
				}
				else
				{
					var myAccount = UnitOfWork.TempAggregatedAccountRepo.GetAllCorporateAggregationAccountByAggregationId(account.Id);
					itme.AccountNumbers = myAccount;
				}

				tempAggregationList.Add(itme);
			}
			return Ok(new ListResponseDTO<TempAggregationResponses>(_data: tempAggregationList, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:GetPendingCorporateCustomerAggregations {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpGet("GetPendingCorporateCustomerAccountAggregation")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public ActionResult<ResponseDTO<TempAggregationResponses>> GetPendingCorporateCustomerAccountAggregation(string corporateAggregateId)
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

			if (string.IsNullOrEmpty(corporateAggregateId))
			{
				return BadRequest("Corporate Customer Account Setup Id is required");
			}

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var Id = Encryption.DecryptGuid(corporateAggregateId);
			var aggregation = UnitOfWork.TempCorporateAggregationRepo.GetCorporateAggregationAccountByAggregationId(Id);
			if (aggregation == null)
			{
				return Ok(new ResponseDTO<TempAggregationResponses>(_data: aggregation, success: true, _message: Message.NotFound));
			}
			var corporateCustomerData = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(aggregation.CorporateCustomerId);
			if (corporateCustomerData == null)
			{
				return BadRequest("Invalid Corporate Customer Id");
			}
			return Ok(new ResponseDTO<TempAggregationResponses>(_data: aggregation, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:GetPendingCorporateCustomerAggregations {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("UpdatePendingCorporateCustomerAccountAggregation")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<TempCorporateAccountAggregationResponse>> UpdatePendingCorporateCustomerAggregation(GenericRequestDto model)
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

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var requestData = JsonConvert.DeserializeObject<CreateAggregateCorporateCustomerModel>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new CreateAggregateCorporateCustomerModel
			{
				Id = requestData.Id,
				CustomerId = requestData.CustomerId,
				CorporateCustomerId = requestData.CorporateCustomerId,
				DefaultAccountNumber = requestData.DefaultAccountNumber,
				DefaultAccountName = requestData.DefaultAccountName,
				AccountNumbers = requestData.AccountNumbers,
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress),
				HostName = Encryption.DecryptStrings(model.HostName)
			};

			var validator = new CreateCorporateAccountAggregationValidation();
			var results = validator.Validate(payload);
			if (!results.IsValid)
			{
				return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
			}

			var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)payload?.CorporateCustomerId);
			if (corporateCustomerDto == null)
			{
				return BadRequest("Corporate customer was not found");
			}

			var entity = UnitOfWork.TempCorporateAggregationRepo.GetCorporateCustomerAggregation(payload.Id, corporateCustomerDto.Id);
			if (entity == null)
			{
				return BadRequest("Invalid id. No Corporate Customer Aggregation record was found");
			}
			if (entity.Status == (int)ProfileStatus.Pending)
			{
				return BadRequest("There's a pending approval for this record. Update is not permitted until it is approved or declined");
			}

			if (entity.InitiatorId != BankProfile.Id)
			{
				return BadRequest("This Request Was Not Initaited By You");
			}
			var oldAggregationAccounts = UnitOfWork.TempAggregatedAccountRepo.GetAllCorporateAggregationAccountByAggregationId(entity.Id);
			//mapList
			var status = (ProfileStatus)entity.Status;
			//{newRelatedAccounts?.ToString()}
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
						$"Aggregation Customer ID: {entity.AccountAggregationId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {nameof(ProfileStatus.Modified)}, Aggregation Accounts: {Formater.JsonType(oldAggregationAccounts)}",
				PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
						$"Aggregation Customer ID: {entity.AccountAggregationId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {status}, Aggregation Accounts: {Formater.JsonType(payload.AccountNumbers)}",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = $"{UserName} has updated corporate customer aggregation record. and is pending approval",
				TimeStamp = DateTime.Now
			};

			var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
			entity.InitiatorId = BankProfile.Id;
			entity.InitiatorUserName = UserName;
			entity.ActionResponseDate = DateTime.Now;
			entity.Status = (int)ProfileStatus.Modified;
			entity.PreviousStatus = entity.Status;
			var newAccountList = new List<TblTempAggregatedAccount>();
			foreach (var account in payload.AccountNumbers)
			{
				var myAccount = new TblTempAggregatedAccount
				{
					Id = Guid.NewGuid(),
					Sn = 0,
					CorporateCustomerId = corporateCustomerDto.Id,
					AccountAggregationId = entity.Id,
					AccountName = account.AccountName,
					AccountNumber = account.AccountNumber,
					DateCreated = DateTime.Now,
					status = 0,
				};
				newAccountList.Add(myAccount);
			}

			//send
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.TempAggregatedAccountRepo.RemoveRange(oldAggregationAccounts);
			UnitOfWork.TempAggregatedAccountRepo.AddRange(newAccountList);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			return Ok(new ResponseDTO<TempCorporateAccountAggregationResponse>(_data: null, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:UpdatePendingCorporateCustomerAggregation {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("UpdateCorporateCustomerAccountAggregation")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<TempCorporateAccountAggregationResponse>> UpdateCorporateCustomerAccountAggregation(GenericRequestDto model)
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

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var requestData = JsonConvert.DeserializeObject<CreateAggregateCorporateCustomerModel>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new CreateAggregateCorporateCustomerModel
			{
				Id = requestData.Id,
				CustomerId = requestData.CustomerId,
				CorporateCustomerId = requestData.CorporateCustomerId,
				DefaultAccountNumber = requestData.DefaultAccountNumber,
				DefaultAccountName = requestData.DefaultAccountName,
				AccountNumbers = requestData.AccountNumbers,
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress),
				HostName = Encryption.DecryptStrings(model.HostName)
			};

			var validator = new CreateCorporateAccountAggregationValidation();
			var results = validator.Validate(payload);
			if (!results.IsValid)
			{
				return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
			}

			var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)payload?.CorporateCustomerId);
			if (corporateCustomerDto == null)
			{
				return BadRequest("Corporate customer was not found");
			}

			var entity = UnitOfWork.CorporateAggregationRepo.GetCorporateCustomerAggregationByID(payload.Id, corporateCustomerDto.Id);
			if (entity == null)
			{
				return BadRequest("Invalid id. No Corporate Customer Aggregation record was found");
			}
			if (entity.Status == (int)ProfileStatus.Pending)
			{
				return BadRequest("There's a pending approval for this record. Update is not permitted until it is approved or declined");
			}

			var oldAggregationAccounts = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAggregateId(entity.Id);
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
						$"Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {nameof(ProfileStatus.Modified)}, Aggregation Accounts:{Formater.JsonType(payload.AccountNumbers)}",
				PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
						$"Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {status}, Aggregation Accounts:  {Formater.JsonType(oldAggregationAccounts)}",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = $"{UserName} has updated corporate customer aggregation record. and is pending approval",
				TimeStamp = DateTime.Now
			};

			var mapTempAggregate = _mapper.Map<TblTempCorporateAccountAggregation>(entity);
			var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
			mapTempAggregate.InitiatorId = BankProfile.Id;
			mapTempAggregate.InitiatorUserName = UserName;
			mapTempAggregate.Id = Guid.NewGuid();
			mapTempAggregate.Sn = 0;
			mapTempAggregate.ActionResponseDate = DateTime.Now;
			mapTempAggregate.Status = (int)ProfileStatus.Modified;
			mapTempAggregate.IsTreated = (int)ProfileStatus.Pending;
			mapTempAggregate.AccountAggregationId = entity.Id;
			entity.Status = (int)ProfileStatus.Pending;
			mapTempAggregate.PreviousStatus = entity.Status;
			mapTempAggregate.Action = nameof(AuditTrailAction.Update).Replace("_", " ");
			var newAccountList = new List<TblTempAggregatedAccount>();
			foreach (var account in payload.AccountNumbers)
			{
				var myAccount = new TblTempAggregatedAccount
				{
					Id = Guid.NewGuid(),
					Sn = 0,
					CorporateCustomerId = corporateCustomerDto.Id,
					AccountAggregationId = entity.Id,
					AccountName = account.AccountName,
					AccountNumber = account.AccountNumber,
					DateCreated = DateTime.Now,
					status = 0,
				};
				newAccountList.Add(myAccount);
			}
			//send
			entity.Status = (int)ProfileStatus.Pending;
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.TempCorporateAggregationRepo.Add(mapTempAggregate);
			UnitOfWork.TempAggregatedAccountRepo.AddRange(newAccountList);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			return Ok(new ResponseDTO<TempCorporateAccountAggregationResponse>(_data: null, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:UpdatePendingCorporateCustomerAggregation {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("AddCorporateCustomerAccountAggregation")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<ResponseDTO<CorporateCustomerResponseDto>> AddCorporateCustomerAggregation(GenericRequestDto model)
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

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var requestData = JsonConvert.DeserializeObject<CreateAggregateCorporateCustomerModel>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new CreateAggregateCorporateCustomerModel
			{
				CorporateCustomerId = requestData.CorporateCustomerId,
				CustomerId = requestData.CustomerId,
				DefaultAccountName = requestData.DefaultAccountName,
				DefaultAccountNumber = requestData.DefaultAccountNumber,
				AccountNumbers = requestData.AccountNumbers,
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress),
				HostName = Encryption.DecryptStrings(model.HostName)
			};

			var validator = new CreateCorporateAccountAggregationValidation();
			var results = validator.Validate(payload);
			if (!results.IsValid)
			{
				return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
			}
			var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)payload?.CorporateCustomerId);
			if (corporateCustomerDto == null)
			{
				return BadRequest("Corporate customer was not found");
			}
			if (!payload.AccountNumbers.Any())
			{
				return BadRequest("No aggregated account selected");
			}
			var tempCorporateAgregation = _mapper.Map<TblTempCorporateAccountAggregation>(payload);
			tempCorporateAgregation.CustomerId = payload.CustomerId;
			tempCorporateAgregation.Id = Guid.NewGuid();

			var tempResult = UnitOfWork.TempCorporateAggregationRepo.CheckDuplicate(tempCorporateAgregation);
			if (tempResult.IsDuplicate != "02")
			{
				return BadRequest(tempResult.Message);
			}

			var mapCorporateAggregate = _mapper.Map<TblCorporateAccountAggregation>(tempCorporateAgregation);
			var result = UnitOfWork.CorporateAggregationRepo.CheckDuplicate(mapCorporateAggregate);
			if (result.IsDuplicate != "02")
			{
				return BadRequest(result.Message);
			}


			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
				$"Aggregation Customer ID: {payload?.CustomerId},Aggregation Accounts: {Formater.JsonType(payload.AccountNumbers)}, Status: {nameof(ProfileStatus.Modified)}",
				PreviousFieldValue = $"",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = $"{UserName} initiated corporate customer Account aggregation",
				TimeStamp = DateTime.Now
			};
			var aggregateAccountList = new List<TblTempAggregatedAccount>();
			foreach (var account in payload.AccountNumbers)
			{
				var newAccount = new TblTempAggregatedAccount
				{
					Id = Guid.NewGuid(),
					Sn = 0,
					AccountAggregationId = tempCorporateAgregation.Id,
					CorporateCustomerId = payload.CorporateCustomerId,
					AccountName = payload.DefaultAccountName,
					AccountNumber = payload.DefaultAccountNumber,
					DateCreated = DateTime.Now
				};
				aggregateAccountList.Add(newAccount);
			}
			tempCorporateAgregation.IsTreated = (int)ProfileStatus.Pending;
			tempCorporateAgregation.InitiatorId = BankProfile.Id;
			tempCorporateAgregation.InitiatorUserName = UserName;
			tempCorporateAgregation.DateInitiated = DateTime.Now;
			tempCorporateAgregation.Status = (int)ProfileStatus.Modified;
			tempCorporateAgregation.Sn = 0;
			tempCorporateAgregation.DateInitiated = DateTime.Now;
			tempCorporateAgregation.Action = nameof(AuditTrailAction.Create).Replace("_", " ");
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.TempCorporateAggregationRepo.Add(tempCorporateAgregation);
			UnitOfWork.TempAggregatedAccountRepo.AddRange(aggregateAccountList);
			UnitOfWork.Complete();
			return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: new(), success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:AddCorporateCustomerAggregation {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("RequestCorporateCustomerAccountAggregationApproval")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<TempCorporateAccountAggregationResponse> RequestAccountSetupApproval(GenericRequestDto model)
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

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new SimpleAction
			{
				Id = requestData.Id,
				CorporateCustomerId = requestData.CorporateCustomerId,
				Reason = requestData.Reason,
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				HostName = Encryption.DecryptStrings(model.HostName),
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress)
			};

			var entity = UnitOfWork.TempCorporateAggregationRepo.GetByIdAsync(payload.Id);
			if (entity == null)
			{
				return BadRequest("Invalid id. No record was found");
			}

			//get corporate customer info
			var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
			//check if corporate Id exist
			if (tblCorporateCustomer == null)
			{
				return BadRequest("Corporate customer was not found");
			}

			if (entity.Status != (int)ProfileStatus.Modified)
			{
				return BadRequest("This record is no longer available for approval request");
			}
			if (entity.InitiatorId != BankProfile.Id)
			{
				return BadRequest("This Request Was not Initiated By you");
			}
			if (!RequestApproval(entity, payload, out string errorMessage))
			{
				return StatusCode(400, errorMessage);
			}
			return Ok(new ListResponseDTO<TempCorporateAccountAggregationResponse>(_data: null, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:RequestCorporateCustomerAggregationApproval {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("ApproveCorporateCustomerAccountAggregationRequest")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<CorporateAccountAggregationResponse> ApproveRequest(GenericRequestDto model)
	{
		string errormsg;
		try
		{
			if (!IsAuthenticated)
			{
				return StatusCode(401, "User is not authenticated");
			}

			if (!IsUserActive(out errormsg))
			{
				return StatusCode(400, errormsg);
			}

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}
			var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new SimpleAction
			{
				Id = requestData.Id,
				CorporateCustomerId = requestData.CorporateCustomerId,
				Reason = requestData.Reason,
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				HostName = Encryption.DecryptStrings(model.HostName),
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress)
			};

			//get corporate customer info
			var tempEntity = UnitOfWork.TempCorporateAggregationRepo.GetByIdAsync(payload.Id);
			if (tempEntity == null)
			{
				return BadRequest("Invalid id. No record was found");
			}
			var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)tempEntity.CorporateCustomerId);
			//check if corporate Id exist
			if (tblCorporateCustomer == null)
			{
				return BadRequest("Corporate customer was not found");
			}
			if (tempEntity.Status != (int)ProfileStatus.Pending)
			{
				return BadRequest("No pending approval request was found for this record");
			}
			if (!ApprovedRequest(tempEntity, payload, out string errorMessage))
			{
				return StatusCode(400, errorMessage);
			}
			return Ok(new ListResponseDTO<TempCorporateAccountAggregationResponse>(_data: null, success: true, _message: Message.Success));

		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:ApproveRequest {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("DeclineCorporateCustomerAccountAggregationRequest")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<CorporateAccountAggregationResponse> DeclineRequest(GenericRequestDto model)
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

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeclineCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}
			var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new SimpleAction
			{
				Id = requestData.Id,
				CorporateCustomerId = requestData.CorporateCustomerId,
				Reason = requestData.Reason,
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				HostName = Encryption.DecryptStrings(model.HostName),
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress)
			};

			if (string.IsNullOrEmpty(payload.Reason))
			{
				return BadRequest("Reason is required");
			}

			var entity = UnitOfWork.TempCorporateAggregationRepo.GetByIdAsync(payload.Id);
			if (entity == null)
			{
				return BadRequest("Invalid id. No record was found");
			}
			//get corporate customer info
			var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
			//check if corporate Id exist
			if (tblCorporateCustomer == null)
			{
				return BadRequest("Corporate customer was not found");
			}
			if (entity.Status == (int)ProfileStatus.Active)
			{
				return BadRequest("This record is already activated");
			}
			//string auditTrailTitle = "Request to reactivate a corporate customer aggregation account";
			if (!DeclineRequest(entity, payload, out string errorMessage))
			{
				return StatusCode(400, errorMessage);
			}
			return Ok(new ListResponseDTO<TempCorporateAccountAggregationResponse>(_data: null, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:RequestCorporateCustomerAggregationReActivation {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("DeactivateCorporateCustomerAggregation")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<CorporateAccountAggregationResponse> DeactivateCorporateCustomerAggregation(GenericRequestDto model)
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

			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeactivateCorporateCustomer))
			{
				return BadRequest("UnAuthorized Access");
			}

			var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}

			var payload = new SimpleAction
			{
				Id = requestData.Id,
				CorporateCustomerId = requestData.CorporateCustomerId,
				Reason = requestData.Reason,
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				HostName = Encryption.DecryptStrings(model.HostName),
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress)
			};

			if (string.IsNullOrEmpty(payload.Reason))
			{
				return BadRequest("Decline Reason is required");
			}
			//var
			//get corporate customer info
			//get CorporateCustomerAggregation by id
			var entity = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(payload.Id);
			if (entity == null)
			{
				return BadRequest("Invalid Id. No record was found");
			}

			var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
			//check if corporate Id exist
			if (tblCorporateCustomer == null)
			{
				return BadRequest("Corporate customer was not found");
			}

			//check workflow is already inactive or pending approval
			if (entity.Status == (int)ProfileStatus.Deactivated)
			{
				return BadRequest("There record is already deactivated");
			}
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {tblCorporateCustomer.CompanyName}, Customer ID: {tblCorporateCustomer.CustomerId}, " +
						$"Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {tblCorporateCustomer.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {tblCorporateCustomer.DefaultAccountNumber}, Status: {nameof(ProfileStatus.Deactivated)}",
				PreviousFieldValue = $"Company Name: {tblCorporateCustomer.CompanyName}, Customer ID: {tblCorporateCustomer.CustomerId}, " +
						$"Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {tblCorporateCustomer.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {tblCorporateCustomer.DefaultAccountNumber}, Status: {nameof(ProfileStatus.Active)}",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = $"{UserName} has deactivated a corporate customer's aggregation account.",
				TimeStamp = DateTime.Now
			};
			entity.Status = (int)ProfileStatus.Deactivated;
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: null, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:DeactivateCorporateCustomerAggregation {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpPost("ReactivateCorporateCustomerAggregation")]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public ActionResult<CorporateAccountAggregationResponse> ReactivateCorporateCustomerAggregation(GenericRequestDto model)
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
			if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateCustomerApproval))
			{
				return BadRequest("UnAuthorized Access");
			}
			var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
			if (requestData == null)
			{
				return BadRequest("invalid request data");
			}
			var payload = new SimpleAction
			{
				Id = requestData.Id,
				CorporateCustomerId = requestData.CorporateCustomerId,
				Reason = requestData.Reason,
				IPAddress = Encryption.DecryptStrings(model.IPAddress),
				HostName = Encryption.DecryptStrings(model.HostName),
				ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
				MACAddress = Encryption.DecryptStrings(model.MACAddress)
			};

			var entity = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(payload.Id);
			if (entity == null)
			{
				return BadRequest("Invalid Id. No record was found");
			}
			//get corporate customer info
			var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
			//check if corporate Id exist
			if (tblCorporateCustomer == null)
			{
				return BadRequest("Corporate customer was not found");
			}
			var checkDuplicate = UnitOfWork.CorporateAggregationRepo.CheckDuplicateAggregate(entity);
			if (!checkDuplicate.IsDuplicate)
			{
				return BadRequest(checkDuplicate.Message);
			}

			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {tblCorporateCustomer.CompanyName}, Customer ID: {tblCorporateCustomer.CustomerId},Authorization Type: {tblCorporateCustomer.AuthorizationType.Replace("_", " ")},Aggregation Customer ID: {entity?.CustomerId}, Aggregation Default Account Name: {tblCorporateCustomer?.DefaultAccountName},Aggregation Default Account Number: {tblCorporateCustomer?.DefaultAccountNumber},Status: {nameof(ProfileStatus.Pending)}  Default Account Name: {tblCorporateCustomer.DefaultAccountName}",
				PreviousFieldValue = $"Company Name: {tblCorporateCustomer.CompanyName}, Customer ID: {tblCorporateCustomer.CustomerId},Authorization Type: {tblCorporateCustomer.AuthorizationType.Replace("_", " ")},Aggregation Customer ID: {entity?.CustomerId}, Aggregation Default Account Name: {tblCorporateCustomer?.DefaultAccountName},Aggregation Default Account Number: {tblCorporateCustomer?.DefaultAccountNumber},Status: {nameof(ProfileStatus.Deactivated)}  Default Account Name: {tblCorporateCustomer.DefaultAccountName}",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = $"{UserName} Initiate Corporate Customer Account Aggregation Reactivation ",
				TimeStamp = DateTime.Now
			};

			var mapTempProfile = Mapper.Map<TblTempCorporateAccountAggregation>(entity);
			mapTempProfile.Id = Guid.NewGuid();
			mapTempProfile.Sn = 0;
			mapTempProfile.CorporateCustomerId = entity.CorporateCustomerId;
			mapTempProfile.AccountAggregationId = entity.Id;
			mapTempProfile.IsTreated = 0;
			mapTempProfile.InitiatorId = BankProfile.Id;
			mapTempProfile.InitiatorUserName = UserName;
			mapTempProfile.DateInitiated = DateTime.Now;
			mapTempProfile.PreviousStatus = entity.Status;
			mapTempProfile.Status = (int)ProfileStatus.Modified;
			mapTempProfile.Action = nameof(TempTableAction.Reactivate).Replace("_", " ");
			mapTempProfile.Reasons = payload.Reason;
			UnitOfWork.TempCorporateAggregationRepo.Add(mapTempProfile);
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: null, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:DeactivateCorporateCustomerAggregation {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	[HttpGet("GetAggregatedCorporateAccounts")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<ListResponseDTO<AggregatedAccountsResponseDto>>> ViewCorporateAccounts(string corporateCustomerId)
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
				return BadRequest("Customer Id is required");
			}

			var customerId = Encryption.DecryptGuid(corporateCustomerId);
			var corporateCustomerData = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(customerId);
			if (corporateCustomerData == null)
			{
				return BadRequest("Invalid Corporate Customer Id");
			}
			var accountAggregrations = UnitOfWork.CorporateAggregationRepo.GetCorporateCustomerAggregations(customerId);
			if (!accountAggregrations.Any())
			{
				return BadRequest("No Aggregated account found");
			}
			var accountList = new List<TblAggregatedAccount>();
			foreach (var account in accountAggregrations)
			{
				var accounts = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAggregateId(account.Id);
				accountList.AddRange(accounts);
			}
			if (!accountList.Any())
			{
				return Ok(new ListResponseDTO<TblAggregatedAccount>(_data: null, success: true, _message: Message.Success));
			}
			return Ok(new ListResponseDTO<TblAggregatedAccount>(_data: accountList, success: true, _message: Message.Success));
		}
		catch (Exception ex)
		{
			_logger.LogError("SERVER ERROR CorporateCustomerAggregationsController:GetAggregatedCorporateAccounts {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
			return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
		}
	}

	private bool ApprovedRequest(TblTempCorporateAccountAggregation profile, SimpleAction payload, out string errorMessage)
	{
		var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile?.CorporateCustomerId);
		if (corporateCustomerDto == null)
		{
			errorMessage = "Corporate customer was not found";
			return false;
		}
		if (profile.Action == nameof(TempTableAction.Create).Replace("_", " "))
		{
			var entity = Mapper.Map<TblCorporateAccountAggregation>(profile);
			if (entity.Status == (int)ProfileStatus.Active)
			{
				errorMessage = "Profile is already active";
				return false;
			}
			var newAccount = UnitOfWork.TempAggregatedAccountRepo.GetAllCorporateAggregationAccountByAggregationId(profile.Id);
			if (!newAccount.Any())
			{
				errorMessage = "No Aggregated Account found";
				return false;
			}
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Authorization Type: {corporateCustomerDto.AuthorizationType.Replace("_", " ")},Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName},Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {nameof(ProfileStatus.Active)}, Aggregated Accounts: {Formater.JsonType(newAccount)}",
				PreviousFieldValue = $"",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = " Approved Corporate Customer Account",
				TimeStamp = DateTime.Now
			};
			entity.Id = Guid.NewGuid();
			profile.IsTreated = (int)ProfileStatus.Active;
			profile.ActionResponseDate = DateTime.Now;
			profile.Status = (int)ProfileStatus.Active;

			entity.Status = (int)ProfileStatus.Active;
			entity.DateCreated = DateTime.Now;
			entity.Sn = 0;
			var newAccountList = new List<TblAggregatedAccount>();
			foreach (var account in newAccount)
			{
				var myAccount = new TblAggregatedAccount
				{
					Id = Guid.NewGuid(),
					Sn = 0,
					CorporateCustomerId = corporateCustomerDto.Id,
					AccountAggregationId = entity.Id,
					AccountName = account.AccountName,
					AccountNumber = account.AccountNumber,
					DateCreated = DateTime.Now,
				};
				newAccountList.Add(myAccount);
			}
			UnitOfWork.CorporateAggregationRepo.Add(entity);
			UnitOfWork.AggregatedAccountRepo.AddRange(newAccountList);

			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(profile);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			errorMessage = "";
			return true;
		}
		if (profile.Action == nameof(TempTableAction.Update).Replace("_", " "))
		{
			var entity = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(profile.AccountAggregationId);
			if (entity == null)
			{
				errorMessage = "Corporate Account Aggregation id";
				return false;
			}

			var previouseAccount = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAggregateId(entity.Id);

			var newAccount = UnitOfWork.TempAggregatedAccountRepo.GetAllCorporateAggregationAccountByAggregationId(profile.Id);
			if (!newAccount.Any())
			{
				errorMessage = "New Account no found";
				return false;
			}

			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
						$"Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {nameof(ProfileStatus.Active)}, Aggregation Accounts:{Formater.JsonType(newAccount)}",
				PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
						$"Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
						$"Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Status: {status}, Aggregation Accounts:  {Formater.JsonType(previouseAccount)}",
				TransactionId = "",
				UserId = BankProfile.Id,
				Username = UserName,
				Description = $"Approved Bank Profile Update. Action was carried out by a Bank user",
				TimeStamp = DateTime.Now
			};

			entity.CustomerId = profile.CustomerId;
			entity.DefaultAccountName = profile.DefaultAccountName;
			entity.DefaultAccountNumber = profile.DefaultAccountNumber;
			var userStatus = UnitOfWork.CorporateAggregationRepo.CheckDuplicate(entity, true);
			if (userStatus.IsDuplicate != "02")
			{
				errorMessage = userStatus.Message;
				return false;
			}
			var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
			profile.IsTreated = (int)ProfileStatus.Active;
			entity.Status = originalStatus;
			profile.ActionResponseDate = DateTime.Now;
			profile.Reasons = payload.Reason;
			var newAccountList = new List<TblAggregatedAccount>();
			foreach (var account in newAccount)
			{
				var myAccount = new TblAggregatedAccount
				{
					Id = Guid.NewGuid(),
					Sn = 0,
					CorporateCustomerId = corporateCustomerDto.Id,
					AccountAggregationId = entity.Id,
					AccountName = account.AccountName,
					AccountNumber = account.AccountNumber,
					DateCreated = DateTime.Now,
				};
				newAccountList.Add(myAccount);
			}

			if (previouseAccount.Any())
			{
				UnitOfWork.AggregatedAccountRepo.RemoveRange(previouseAccount);
			}
			UnitOfWork.AggregatedAccountRepo.AddRange(newAccountList);
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(profile);
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			errorMessage = "";
			return true;
		}
		if (profile.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
		{
			var entity = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(profile.AccountAggregationId);
			if (entity == null)
			{
				errorMessage = "Corporate Account Aggregation id";
				return false;
			}
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
					$"Authorization Type: {corporateCustomerDto.AuthorizationType.Replace("_", " ")}, Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
					$"Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Email: {corporateCustomerDto.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
				PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, " +
					$"Authorization Type: {corporateCustomerDto.AuthorizationType.Replace("_", " ")}, Default Account Name: {corporateCustomerDto.DefaultAccountName}, " +
					$"Default Account Number: {corporateCustomerDto.DefaultAccountNumber}, Email: {corporateCustomerDto.Email1}",
				UserId = BankProfile.Id,
				Username = UserName,
				Description = $"Approved Bank Profile Reactivation. Action was carried out by a Bank user",
				TimeStamp = DateTime.Now
			};

			entity.Status = (int)ProfileStatus.Active;
			profile.Status = (int)ProfileStatus.Active;
			profile.IsTreated = (int)ProfileStatus.Active;
			//profile.ApprovedId = BankProfile.Id;
			//profile.ApprovalUsername = UserName;
			profile.ActionResponseDate = DateTime.Now;
			profile.Reasons = "";
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(profile);
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			errorMessage = "";
			return true;
		}
		errorMessage = "Unknow Request";
		return false;
	}
	private bool RequestApproval(TblTempCorporateAccountAggregation entity, SimpleAction payload, out string errorMessage)
	{
		var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity?.CorporateCustomerId);
		if (corporateCustomerDto == null)
		{
			errorMessage = "Corporate customer was not found";
			return false;
		}
		var emailNotification = new EmailNotification
		{
			CompanyName = corporateCustomerDto.CompanyName,
			CustomerId = corporateCustomerDto.CustomerId,
			Action = entity.Action,
		};
		if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
		{
			if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
			{
				errorMessage = "Profile wasn't Decline or modified initially";
				return false;
			}

			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",
				PreviousFieldValue = "",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = $"{UserName} initiated corporate customer's aggregation account. deactivation",
				TimeStamp = DateTime.Now
			};

			//email notification
			var newAccount = UnitOfWork.TempAggregatedAccountRepo.GetAllCorporateAggregationAccountByAggregationId(entity.Id);
			if (!newAccount.Any())
			{
				errorMessage = "No Aggregated Account found";
				return false;
			}
			emailNotification.AggregatedAccounts = Formater.JsonType(newAccount);
			entity.Status = (int)ProfileStatus.Pending;
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			notify.NotifyBankAdminAuthorizerForCorporateCustomerAggregationApproval(emailNotification);
			errorMessage = "Request Approval Was Successful";
			return true;
		}
		if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
		{
			var profile = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(entity.AccountAggregationId);
			if (profile == null)
			{
				errorMessage = "Corporate Account Aggregation id";
				return false;
			}

			if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
			{
				errorMessage = "Profile wasn't Decline or modified initially";
				return false;
			}

			if (entity.Status == (int)ProfileStatus.Pending)
			{
				//errorMessage = "Profile wasn't Decline or modified initially";
				errorMessage = "There is a pending request awaiting Approval";
				return false;
			}
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",

				PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {profile.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = "Modified Corporate Customer Info By Bank Admin",
				TimeStamp = DateTime.Now
			};

			//update status
			entity.Status = (int)ProfileStatus.Pending;
			profile.Status = (int)ProfileStatus.Pending;
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(profile);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			//notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity, emailNotification);
			errorMessage = "Request Approval Was Successful";
			return true;
		}
		if (entity.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
		{
			var profile = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(entity.AccountAggregationId);
			if (profile == null)
			{
				errorMessage = "Corporate Account Aggregation id";
				return false;
			}

			if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
			{
				errorMessage = "Profile wasn't Decline or modified initially";
				return false;
			}

			if (entity.Status == (int)ProfileStatus.Pending)
			{
				errorMessage = "There is a pending request awaiting Approval";
				return false;
			}
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",

				PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {profile.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",
				TransactionId = "",
				UserId = Guid.Parse(UserRoleId),
				Username = UserName,
				Description = "Modified Corporate Customer Info By Bank Admin",
				TimeStamp = DateTime.Now
			};

			//update status
			entity.Status = (int)ProfileStatus.Pending;
			profile.Status = (int)ProfileStatus.Pending;
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(profile);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			//notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity, emailNotification);
			errorMessage = "Request Approval Was Successful";
			return true;
		}
		errorMessage = "invalid Request";
		return false;
	}
	private bool DeclineRequest(TblTempCorporateAccountAggregation entity, SimpleAction payload, out string errorMessage)
	{
		var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity?.CorporateCustomerId);
		if (corporateCustomerDto == null)
		{
			errorMessage = "Corporate customer was not found";
			return false;
		}
		var initiatorProfile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);
		var emailNotification = new EmailNotification
		{
			CustomerId = entity.CustomerId,
			Email = corporateCustomerDto.CorporateEmail,
			AccountName = corporateCustomerDto.DefaultAccountName,
			AccountNumber = corporateCustomerDto.DefaultAccountNumber,
			Action = entity.Action,
			Reason = payload.Reason
		};
		if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
		{
			if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
			{
				errorMessage = "Profile wasn't Decline or modified initially";
				return false;
			}
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {entity.CustomerId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",
				PreviousFieldValue = "",
				TransactionId = "",
				UserId = BankProfile.Id,
				Username = UserName,
				Description = $"{UserName} Decline Approval for  corporate customer's aggregation account. Action was carried out by a Bank user",
				TimeStamp = DateTime.Now
			};


			//update status
			entity.Status = (int)ProfileStatus.Declined;
			entity.IsTreated = (int)ProfileStatus.Declined;
			entity.Reasons = payload.Reason;
			//entity.ApprovedId = BankProfile.Id;
			//entity.ApprovalUsername = UserName;
			entity.ActionResponseDate = DateTime.Now;
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();
			notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
			errorMessage = "Decline Approval Was Successful";
			return true;
		}
		if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
		{
			var profile = UnitOfWork.CorporateAggregationRepo.GetByIdAsync(entity.AccountAggregationId);
			if (profile == null)
			{
				errorMessage = "Invalid Bank Profile id";
				return false;
			}

			if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
			{
				errorMessage = "Profile wasn't Decline or modified initially";
				return false;
			}
			var status = (ProfileStatus)entity.Status;
			var auditTrail = new TblAuditTrail
			{
				Id = Guid.NewGuid(),
				ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
				Ipaddress = payload.IPAddress,
				Macaddress = payload.MACAddress,
				HostName = payload.HostName,
				ClientStaffIpaddress = payload.ClientStaffIPAddress,
				NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {entity.AccountAggregationId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",
				PreviousFieldValue = "",
				TransactionId = "",
				UserId = BankProfile.Id,
				Username = UserName,
				Description = $"Decline Approval to Update Corporate Customer Information. Action was carried out by a Bank user",
				TimeStamp = DateTime.Now
			};

			entity.Status = (int)ProfileStatus.Declined;
			profile.Status = (int)entity.PreviousStatus;
			entity.IsTreated = (int)ProfileStatus.Declined;
			entity.Reasons = payload.Reason;
			//entity.ApprovedId = BankProfile.Id;
			//entity.ApprovalUsername = UserName;
			entity.ActionResponseDate = DateTime.Now;
			UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
			UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(profile);
			UnitOfWork.AuditTrialRepo.Add(auditTrail);
			UnitOfWork.Complete();

			notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
			errorMessage = "Decline Approval Was Successful";
			return true;
		}
		if (entity.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
		{
			if (entity.CorporateCustomerId != null)
			{
				var profile = UnitOfWork.CorporateAggregationRepo.GetCorporateAggregationByAggregationCustomerId(entity.AccountAggregationId);
				if (profile == null)
				{
					errorMessage = "Invalid Bank Profile id";
					return false;
				}

				if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
				{
					errorMessage = "Profile wasn't Decline or modified initially";
					return false;
				}
				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Aggregation Customer ID: {entity.AccountAggregationId}, Aggregation Default Account Name: {corporateCustomerDto.DefaultAccountName}, Aggregation Default Account Number: {corporateCustomerDto.DefaultAccountNumber},Active Status: {status}",
					PreviousFieldValue = $"",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Request for Bank Profile Reactivation. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				//update status
				entity.Status = (int)ProfileStatus.Declined;
				profile.Status = (int)entity.PreviousStatus;
				entity.IsTreated = (int)ProfileStatus.Declined;
				entity.Reasons = payload.Reason;
				//entity.ApprovedId = BankProfile.Id;
				//entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				UnitOfWork.TempCorporateAggregationRepo.UpdateAccountAggregation(entity);
				UnitOfWork.CorporateAggregationRepo.UpdateAccountAggregation(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
				errorMessage = "Decline Request Was Successful";
				return true;
			}
			errorMessage = "Invalid Corporate Customer Id is Require";
			return false;
		}
		errorMessage = "invalid Request";
		return false;
	}
}



