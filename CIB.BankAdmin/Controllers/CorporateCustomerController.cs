using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateCustomer.Mapper;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.BankAdmin.Controllers
{
	[ApiController]
	[Route("api/BankAdmin/v1/[controller]")]
	public class CorporateCustomerController : BaseAPIController
	{
		private readonly IApiService _apiService;
		private readonly IEmailService _emailService;
		private readonly ILogger<CorporateCustomerController> _logger;
		private readonly IConfiguration _config;
		protected readonly INotificationService notify;
		public CorporateCustomerController(INotificationService notify, IConfiguration config, IEmailService emailService, ILogger<CorporateCustomerController> _logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IApiService apiService, IAuthenticationService authService) : base(mapper, unitOfWork, accessor, authService)
		{
			this._apiService = apiService;
			this._logger = _logger;
			this._emailService = emailService;
			this._config = config;
			this.notify = notify;
		}

		[HttpGet("GetCorporateCustomers")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ListResponseDTO<CorporateCustomerResponseDto>> GetCorporateCustomers()
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

				if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
				{
					return BadRequest("UnAuthorized Access");
				}

				var corporateCustomers = UnitOfWork.CorporateCustomerRepo.GetAllCorporateCustomers().ToList();
				if (corporateCustomers == null || corporateCustomers?.Count == 0)
				{
					return StatusCode(204);
				}
				//UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail {Id = Guid.NewGuid(), Username = UserName, Action = "Get Corporate Customers", Usertype = "", TimeStamp = DateTime.Now, PageName = "", Channel = "web" });
				//UnitOfWork.Complete();
				return Ok(new ListResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<List<CorporateCustomerResponseDto>>(corporateCustomers), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpGet("GetCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<CorporateCustomerResponseDto> GetCorporateCustomer(string id)
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
				if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
				{
					return BadRequest("UnAuthorized Access");
				}
				var CustomerId = Encryption.DecryptGuid(id);
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(CustomerId);
				if (corporateCustomer == null)
				{
					return BadRequest("Invalid id. Corporate customer not found");
				}
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(corporateCustomer), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("CreateCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<CorporateCustomerResponseDto> CreateCorporateCustomer(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateCustomer))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<CreateCorporateCustomerRequestDto>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new CreateCorporateCustomerRequestDto
				{
					CompanyName = requestData.CompanyName,
					Email1 = requestData.Email1,
					CustomerId = requestData.CustomerId,
					DefaultAccountNumber = requestData.DefaultAccountNumber,
					DefaultAccountName = requestData.DefaultAccountName,
					AuthorizationType = requestData.AuthorizationType,
					PhoneNumber = requestData.PhoneNumber,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};

				var validator = new CorporateCustomerValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}


				var mapProfile = Mapper.Map<TblCorporateCustomer>(payload);
				var customerStatus = UnitOfWork.CorporateCustomerRepo.CheckDuplicate(mapProfile);
				if (customerStatus.IsDuplicate != "02")
				{
					return StatusCode(400, customerStatus.Message);
				}


				mapProfile.Status = 0;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					NewFieldValue = $"Company Name: {payload.CompanyName}, Company Email: {payload.Email1}, CustomerId: {payload.CustomerId}, Account Number: {payload.DefaultAccountNumber}, Account Name: {payload.DefaultAccountName}, AuthorizationType: {payload.AuthorizationType}, Phone Number: {payload.PhoneNumber}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Create Corporate Customer by bank admin",
					TimeStamp = DateTime.Now
				};
				//send email to autorizer 
				var mapCorporateCustomer = Mapper.Map<TblTempCorporateCustomer>(payload);
				mapCorporateCustomer.Status = (int)ProfileStatus.Modified;
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.TemCorporateCustomerRepo.Add(mapCorporateCustomer);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(mapProfile), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("UpdateCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<CorporateCustomerResponseDto> UpdateCorporateCustomer(GenericRequestDto model)
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
				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<UpdateCorporateCustomerRequestDto>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new UpdateCorporateCustomerRequestDto
				{
					Id = requestData.Id,
					CompanyName = requestData.CompanyName,
					CorporateShortName = requestData.CorporateShortName,
					Email1 = requestData.Email1,
					CustomerId = requestData.CustomerId,
					DefaultAccountNumber = requestData.DefaultAccountNumber,
					DefaultAccountName = requestData.DefaultAccountName,
					AuthorizationType = requestData.AuthorizationType,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress)
				};

				var validator = new UpdateCorporateCustomerValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}
				if (entity.Status == 0)
				{
					return BadRequest("A pending approval has been detected. Update is not permitted until approval is done");
				}

				var checkTempForShortName = UnitOfWork.TemCorporateCustomerRepo.GetCorporateCustomerByCustomerByShortName(payload.CorporateShortName);
				if (checkTempForShortName != null)
				{
					if (checkTempForShortName.IsTreated == (int)ProfileStatus.Pending)
					{
						return BadRequest("Customer with the same corporate short name is awaiting approval");
					}
					if (checkTempForShortName.IsTreated == (int)ProfileStatus.Active)
					{
						return BadRequest("Customer with the same corporate short name already exist");
					}
				}

				var checkForDeplicateShortName = UnitOfWork.CorporateCustomerRepo.CheckDuplicateCorporateShortName(payload.CorporateShortName);
				if (checkForDeplicateShortName != null)
				{
					if (checkForDeplicateShortName.Status == (int)ProfileStatus.Pending)
					{
						return BadRequest("Customer with the same corporate short name is awaiting approval");
					}
					if (checkForDeplicateShortName.Status == (int)ProfileStatus.Deactivated)
					{
						return BadRequest("Customer with the same corporate short name is deactivated");
					}
					if (checkForDeplicateShortName.Status == (int)ProfileStatus.Active)
					{
						return BadRequest("Customer with the same corporate short name already exist");
					}
				}



				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {payload.CompanyName}, Customer ID: {payload.CustomerId}, " +
						$"Authorization Type: {payload.AuthorizationType.Replace("_", " ")}, Default Account Name: {payload.DefaultAccountName}, " +
						$"Default Account Number: {payload.DefaultAccountNumber}, Email: {payload.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Modified Corporate Customer Info By Bank Admin",
					TimeStamp = DateTime.Now
				};
				entity.CompanyName = payload.CompanyName;
				entity.CustomerId = payload.CustomerId;
				entity.Email1 = payload.Email1;
				entity.DefaultAccountName = payload.DefaultAccountName;
				entity.DefaultAccountNumber = payload.DefaultAccountNumber;
				entity.AuthorizationType = payload.AuthorizationType;
				entity.Status = (int)ProfileStatus.Modified;
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(entity), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("UpdateCorporateCustomerShortName")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<CorporateCustomerResponseDto> UpdateCorporateCustomerShortName(GenericRequestDto model)
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
				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<UpdateCorporateCustomerShortNameRequest>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new UpdateCorporateCustomerShortNameRequestDto
				{
					Id = Guid.Parse(requestData.Id),
					CorporateShortName = requestData.CorporateShortName,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};

				var validator = new UpdateCorporateCustomerShortNameValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}
				if (entity.Status == 0)
				{
					return BadRequest("A pending approval has been detected. Update is not permitted until approval is done");
				}

				var checkTempForShortName = UnitOfWork.TemCorporateCustomerRepo.GetCorporateCustomerByCustomerByShortName(payload.CorporateShortName);
				if (checkTempForShortName != null)
				{
					if (checkTempForShortName.IsTreated == (int)ProfileStatus.Pending)
					{
						return BadRequest("Customer with the same corporate short name is awaiting approval");
					}
					//if (checkTempForShortName.IsTreated == (int)ProfileStatus.Active)
					//{
					//	return BadRequest("Customer with the same corporate short name already exist");
					//}
				}

				var checkForDeplicateShortName = UnitOfWork.CorporateCustomerRepo.CheckDuplicateCorporateShortName(payload.CorporateShortName);
				if (checkForDeplicateShortName != null)
				{
					if (checkForDeplicateShortName.Status == (int)ProfileStatus.Pending)
					{
						return BadRequest("Customer with the same corporate short name is awaiting approval");
					}
					if (checkForDeplicateShortName.Status == (int)ProfileStatus.Deactivated)
					{
						return BadRequest("Customer with the same corporate short name is deactivated");
					}
					if (checkForDeplicateShortName.Status == (int)ProfileStatus.Active)
					{
						return BadRequest("Customer with the same corporate short name already exist");
					}
				}

				var comporateProfiles = UnitOfWork.CorporateProfileRepo.GetProfileByCorporateCustomerId(entity.Id);
				if (!comporateProfiles.Any())
				{
					return BadRequest("No Profile is associated with the corporate customers");
				}

				// foreach (var profile in comporateProfiles)
				// {
				// 	profile.Status = (int)ProfileStatus.Pending;
				// 	UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
				// }


				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Corporate Short Name: {payload.CorporateShortName}, Customer ID: {entity.CustomerId},Status: {status} ",
					PreviousFieldValue = $"Company Name: {entity.CorporateShortName}, Customer ID: {entity.CustomerId}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Modified Corporate Customer short name Info By Bank Admin",
					TimeStamp = DateTime.Now
				};

				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
				var mapCustomer = Mapper.Map<TblTempCorporateCustomer>(entity);
				mapCustomer.CorporateShortName = payload.CorporateShortName;
				mapCustomer.Id = Guid.NewGuid();
				mapCustomer.Sn = 0;
				mapCustomer.CorporateCustomerId = entity.Id;
				entity.Status = originalStatus;
				mapCustomer.Action = nameof(TempTableAction.Update_Corporate_short_Name).Replace("_", " ");
				mapCustomer.IsTreated = (int)ProfileStatus.Pending;
				mapCustomer.InitiatorId = BankProfile.Id;
				mapCustomer.InitiatorUsername = UserName;
				mapCustomer.DateRequested = DateTime.Now;
				mapCustomer.PreviousStatus = entity.Status;
				mapCustomer.Status = (int)ProfileStatus.Modified;
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.TemCorporateCustomerRepo.Add(mapCustomer);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(entity), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("CorporateCustomerApprovalRequest")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<CorporateCustomerResponseDto> CorporateCustomerApprovalRequest(GenericRequestDto model)
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
				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new SimpleAction
				{
					Id = requestData.Id,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};
				var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}

				if (entity.Status == 1) return BadRequest("Profile was not declined or modified initially");

				if (entity.InitiatorId != BankProfile.Id)
				{
					return BadRequest("This Request Was not Initiated By you");
				}

				if (!RequestApproval(entity, payload, out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}

				if (entity.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
				{
					return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: new CorporateCustomerResponseDto(), success: true, _message: Message.Success));
				}

				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(profile), success: true, _message: "ok"));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("ApproveCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<TblCorporateCustomer> ApproveOrActivateCorporateCustomer(GenericRequestDto model)
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

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new SimpleAction
				{
					Id = requestData.Id,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};
				var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}

				//if (entity.Status == ) return BadRequest("Customer onboarding is yet to be completed. Customer's account limit may not have been set");

				if (entity.Status == (int)ProfileStatus.Active)
				{
					return BadRequest("Customer is already approved");
				}


				if (!ApprovedRequest(entity, payload, out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}

				if (entity.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
				{
					return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: new CorporateCustomerResponseDto(), success: true, _message: Message.Success));
				}


				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(profile), success: true, _message: Message.ProfileActivated));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}
		[HttpPost("DeclineCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<CorporateCustomerResponseDto>> DeclineCorporateCustomerStatus(GenericRequestDto model)
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

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}
				var payload = new AppActionDto
				{
					Id = requestData.Id,
					Reason = requestData.Reason,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};
				if (string.IsNullOrEmpty(payload.Reason))
				{
					BadRequest("Reason for declining is required");
				}
				//check if corporate Id exist
				var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}

				if (!DeclineRequest(entity, payload, out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}

				if (entity.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
				{
					return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: new CorporateCustomerResponseDto(), success: true, _message: Message.Success));
				}

				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);

				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(profile), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}
		[HttpPost("DeactivateCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<CorporateCustomerResponseDto>> DeactivateCorporateCustomerStatus(GenericRequestDto model)
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


				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new AppActionDto
				{
					Id = requestData.Id,
					Reason = requestData.Reason,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};
				if (string.IsNullOrEmpty(payload.Reason))
				{
					BadRequest("Reason for declining is required");
				}
				//c
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}
				if (entity.Status == -1) return BadRequest("Customer is already de-activated");

				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Deactivated Corporate Customer Account",
					TimeStamp = DateTime.Now
				};
				var checkDuplicateRequest = UnitOfWork.TemCorporateCustomerRepo.CheckDuplicateRequest(entity, nameof(TempTableAction.Deactivate).Replace("_", " "));
				if (checkDuplicateRequest.Count != 0)
				{
					return BadRequest("There is a pending request awaiting Approval");
				}


				var mapTempProfile = Mapper.Map<TblTempCorporateCustomer>(entity);
				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.Sn = 0;
				mapTempProfile.CorporateCustomerId = entity.Id;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.PreviousStatus = entity.Status;
				mapTempProfile.Status = (int)ProfileStatus.Deactivated;
				mapTempProfile.Action = nameof(TempTableAction.Deactivate).Replace("_", " ");
				mapTempProfile.Reasons = payload.Reason;
				mapTempProfile.IsTreated = (int)ProfileStatus.Deactivated;
				//new modification
				entity.Status = (int)ProfileStatus.Deactivated;
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				// new modification end
				UnitOfWork.TemCorporateCustomerRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(entity), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}
		[HttpPost("ReactivateCorporateCustomer")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<CorporateCustomerResponseDto>> ReactivateCorporateCustomers(GenericRequestDto model)
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

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}
				//check if corporate Id exist
				var payload = new SimpleAction
				{
					Id = requestData.Id,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};
				//var customerId = Encryption.DecryptGuid(payload.Id);
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id. Customer does not exist");
				}
				if (entity.Status != -1) return BadRequest("Customer was not deactivated");

				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Reactivated Corporate Customer Account",
					TimeStamp = DateTime.Now
				};

				var checkDuplicateRequest = UnitOfWork.TemCorporateCustomerRepo.CheckDuplicateRequest(entity, nameof(TempTableAction.Reactivate).Replace("_", " "));
				if (checkDuplicateRequest.Count != 0)
				{
					return BadRequest("There is a pending request awaiting Approval");
				}


				var mapTempProfile = Mapper.Map<TblTempCorporateCustomer>(entity);

				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.Sn = 0;
				mapTempProfile.CorporateCustomerId = entity.Id;
				mapTempProfile.IsTreated = 0;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.PreviousStatus = entity.Status;
				mapTempProfile.Status = (int)ProfileStatus.Pending;
				mapTempProfile.Action = nameof(TempTableAction.Reactivate).Replace("_", " ");
				mapTempProfile.Reasons = payload.Reason;

				UnitOfWork.TemCorporateCustomerRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(entity), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}
		[HttpPost("UpdateAccountLimit")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<CorporateCustomerResponseDto>> UpdateAccountLimitModel(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateCustomerAccountLimit))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<UpdateAccountLimitRequestDto>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new UpdateAccountLimitRequestDto
				{
					IsApprovalByLimit = requestData.IsApprovalByLimit,
					CorporateCustomerId = requestData.CorporateCustomerId,
					MinAccountLimit = requestData.MinAccountLimit,
					MaxAccountLimit = requestData.MaxAccountLimit,
					SingleTransDailyLimit = requestData.SingleTransDailyLimit,
					BulkTransDailyLimit = requestData.BulkTransDailyLimit,
					AuthenticationLimit = requestData.AuthenticationLimit,
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var validator = new CreateLimitCorporateCustomerValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				if (payload.MaxAccountLimit > payload.SingleTransDailyLimit)
				{
					return BadRequest("Maximum Limit per Transaction can not be greater than accumulative single daily Limit");
				}

				if (payload.MinAccountLimit < 1)
				{
					return BadRequest("MinAccountLimit can not be less than 1");
				}

				if (payload.MaxAccountLimit > payload.BulkTransDailyLimit)
				{
					return BadRequest("Maximum Limit per Transaction can not be greater than accumulative bulk daily Limit");
				}

				//check if corporate customer Id exist
				var entity = UnitOfWork.CorporateCustomerRepo.GetCorporateCustomerByCustomerID(payload.CorporateCustomerId);
				if (entity == null)
				{
					return BadRequest("Invalid Corporate Customer ID");
				}

				//if (entity.Status == (int)ProfileStatus.Pending || entity.Status == (int)ProfileStatus.Modified)
				//{
				//	return BadRequest("There is already  pending modification or approval for account limit update");
				//}

				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Maximum Account Limit: {payload.MaxAccountLimit}, Minimum Account Limit: {payload.MinAccountLimit}, Single Transaction Daily Limit: {payload.SingleTransDailyLimit}, Bulk Transaction Daily Limit: {payload.BulkTransDailyLimit} Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit},Single Transaction Daily Limit: {entity.SingleTransDailyLimit}, Bulk Transaction Daily Limit: {entity.BulkTransDailyLimit} Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Updated Account Limit of Corporate Customer",
					TimeStamp = DateTime.Now
				};

				var mapCustomer = Mapper.Map<TblTempCorporateCustomer>(entity);
				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Modified;

				mapCustomer.Id = Guid.NewGuid();
				mapCustomer.Sn = 0;
				mapCustomer.CorporateCustomerId = entity.Id;
				mapCustomer.IsTreated = (int)ProfileStatus.Pending;
				mapCustomer.InitiatorId = BankProfile.Id;
				mapCustomer.InitiatorUsername = UserName;
				mapCustomer.IsApprovalByLimit = payload.IsApprovalByLimit ? 1 : 0;
				mapCustomer.MinAccountLimit = payload.MinAccountLimit;
				mapCustomer.MaxAccountLimit = payload.MaxAccountLimit;
				mapCustomer.SingleTransDailyLimit = payload.SingleTransDailyLimit;
				mapCustomer.BulkTransDailyLimit = payload.BulkTransDailyLimit;
				mapCustomer.AuthenticationLimit = payload.AuthenticationLimit;
				mapCustomer.Action = nameof(TempTableAction.Update_Account_limit).Replace("_", " ");
				mapCustomer.PreviousStatus = entity.Status;
				entity.Status = originalStatus;
				mapCustomer.Status = (int)ProfileStatus.Modified;
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.TemCorporateCustomerRepo.Add(mapCustomer);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(entity), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("UpdateTempAccountLimit")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<CorporateCustomerResponseDto>> UpdateTempAccountLimit(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateCustomerAccountLimit))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<UpdateAccountLimitRequestDto>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}
				var payload = new UpdateAccountLimitRequestDto
				{
					IsApprovalByLimit = requestData.IsApprovalByLimit,
					CorporateCustomerId = requestData.CorporateCustomerId,
					MinAccountLimit = requestData.MinAccountLimit,
					MaxAccountLimit = requestData.MaxAccountLimit,
					SingleTransDailyLimit = requestData.SingleTransDailyLimit,
					BulkTransDailyLimit = requestData.BulkTransDailyLimit,
					AuthenticationLimit = requestData.AuthenticationLimit,
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var validator = new CreateLimitCorporateCustomerValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				if (payload.MaxAccountLimit > payload.SingleTransDailyLimit)
				{
					return BadRequest("Maximum Limit per Transaction can not be greater than accumulative single daily Limit");
				}

				if (payload.MinAccountLimit < 1)
				{
					return BadRequest("MinAccountLimit can not be less than 1");
				}

				if (payload.MaxAccountLimit > payload.BulkTransDailyLimit)
				{
					return BadRequest("Maximum Limit per Transaction can not be greater than accumulative bulk daily Limit");
				}

				//check if corporate customer Id exist
				var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(Guid.Parse(payload.CorporateCustomerId));
				if (entity == null)
				{
					return BadRequest("Invalid Corporate Customer ID");
				}

				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Maximum Account Limit: {payload.MaxAccountLimit}, Minimum Account Limit: {payload.MinAccountLimit}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Updated Account Limit of Corporate Customer",
					TimeStamp = DateTime.Now
				};


				entity.IsApprovalByLimit = payload.IsApprovalByLimit ? 1 : 0;
				entity.MinAccountLimit = payload.MinAccountLimit;
				entity.MaxAccountLimit = payload.MaxAccountLimit;
				entity.SingleTransDailyLimit = payload.SingleTransDailyLimit;
				entity.BulkTransDailyLimit = payload.BulkTransDailyLimit;
				entity.Status = (int)ProfileStatus.Modified;
				entity.Action = nameof(TempTableAction.Update_Account_limit).Replace("_", " ");
				entity.Status = (int)ProfileStatus.Modified;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateCustomerResponseDto>(_data: Mapper.Map<CorporateCustomerResponseDto>(entity), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpGet("CorporateCustomerPendingApproval")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ListResponseDTO<TblTempCorporateCustomer>> GetCorporateCustomerPendingApproval()
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

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
				{
					return BadRequest("UnAuthorized Access");
				}

				var entity = UnitOfWork.TemCorporateCustomerRepo.GetCorporateCustomerPendingApproval((int)ProfileStatus.Pending);
				return Ok(new ListResponseDTO<TblTempCorporateCustomer>(_data: entity, success: true, _message: Message.Success));

			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
				{
					_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("BulkRequestApproved")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
		public ActionResult<bool> BulkRequestApproved(GenericRequestDto model)
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
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveCorporateUserProfile))
				{
					return BadRequest("UnAuthorized Access");
				}

				var responseErrors = new List<BulkError>();
				foreach (var item in requestData)
				{

					var payload = new SimpleAction
					{
						Id = item.Id,
						IPAddress = Encryption.DecryptStrings(model.IPAddress),
						HostName = Encryption.DecryptStrings(model.HostName),
						ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
						MACAddress = Encryption.DecryptStrings(model.MACAddress)
					};
					var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
					if (entity == null)
					{
						var bulkError = new BulkError
						{
							Message = "Invalid Id. Customer does not exist",
							ActionInfo = $"CorporateCustomerID : {payload.Id}"
						};
						responseErrors.Add(bulkError);
					}
					else
					{
						if (entity.Status == (int)ProfileStatus.Active)
						{
							var bulkError = new BulkError
							{
								Message = "Customer is already approved",
								ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
							};
							responseErrors.Add(bulkError);
						}
						else
						{
							if (!ApprovedRequest(entity, payload, out string errorMessage))
							{
								var bulkError = new BulkError
								{
									Message = errorMessage,
									ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
								};
								responseErrors.Add(bulkError);
							}
						}
					}
				}
				if (responseErrors.Any())
				{
					var errorResult = new
					{
						Message = "An error has occurred while processing the Bulk Request. Approved",
						Data = responseErrors
					};
					return BadRequest(errorResult);
				}
				return Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("BulkRequestDecline")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
		public ActionResult<bool> BulkRequestDecline(GenericRequestDto model)
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
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}
				var responseErrors = new List<BulkError>();
				foreach (var item in requestData)
				{
					var payload = new AppActionDto
					{
						Id = item.Id,
						Reason = item.Reason,
						IPAddress = Encryption.DecryptStrings(model.IPAddress),
						ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
						HostName = Encryption.DecryptStrings(model.HostName)
					};
					var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
					if (entity == null)
					{
						var bulkError = new BulkError
						{
							Message = "Invalid Id. Customer does not exist",
							ActionInfo = $"CorporateCustomerID : {payload.Id}"
						};
						responseErrors.Add(bulkError);
					}
					else
					{
						if (entity.Status == (int)ProfileStatus.Active)
						{
							var bulkError = new BulkError
							{
								Message = "Customer is already approved",
								ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
							};
							responseErrors.Add(bulkError);
						}
						else
						{
							if (!DeclineRequest(entity, payload, out string errorMessage))
							{
								var bulkError = new BulkError
								{
									Message = errorMessage,
									ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
								};
								responseErrors.Add(bulkError);
							}
						}
					}
				}
				if (responseErrors.Any())
				{
					var errorResult = new
					{
						Message = "An error has occurred while processing the Bulk Decline Request.",
						Data = responseErrors
					};
					return BadRequest(errorResult);
				}
				return Ok(true);
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("BulkRequestApproval")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
		public ActionResult<bool> BulkRequestApproval(GenericRequestDto model)
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
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var responseErrors = new List<BulkError>();
				foreach (var item in requestData)
				{
					var payload = new SimpleAction
					{
						Id = item.Id,
						IPAddress = Encryption.DecryptStrings(model.IPAddress),
						HostName = Encryption.DecryptStrings(model.HostName),
						ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
						MACAddress = Encryption.DecryptStrings(model.MACAddress)
					};
					var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
					if (entity == null)
					{
						var bulkError = new BulkError
						{
							Message = "Invalid Id. Customer does not exist",
							ActionInfo = $"UserName : {payload.Id}, Action: {entity.Action}",
							Id = payload.Id,
						};
						responseErrors.Add(bulkError);
					}
					else
					{
						if (entity.Status == 1)
						{
							var bulkError = new BulkError
							{
								Message = "Profile was not declined or modified initially",
								ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}",
								Id = entity.CorporateCustomerId
							};
							responseErrors.Add(bulkError);
						}
						else
						{

							if (entity.InitiatorId != BankProfile.Id)
							{
								var bulkError = new BulkError
								{
									Message = "This Request Was not Initiated By you",
									ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}",
									Id = entity.CorporateCustomerId
								};
								responseErrors.Add(bulkError);
							}
							else
							{
								if (!RequestApproval(entity, payload, out string errorMessage))
								{
									var bulkError = new BulkError
									{
										Message = errorMessage,
										ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}",
										Id = entity.CorporateCustomerId
									};
									responseErrors.Add(bulkError);
								}
							}
						}

					}
				}
				if (responseErrors.Any())
				{
					var errorResult = new
					{
						Message = "An error has occurred while processing the Bulk Approval Request.",
						Data = responseErrors
					};
					return BadRequest(errorResult);
				}
				return Ok(true);
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		private bool ApprovedRequest(TblTempCorporateCustomer profile, SimpleAction payload, out string errorMessage)
		{

			if (profile.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
			{
				var entity = Mapper.Map<TblCorporateCustomer>(profile);
				var userStatus = UnitOfWork.CorporateCustomerRepo.CheckDuplicate(entity);
				if (userStatus.IsDuplicate != "02")
				{
					errorMessage = userStatus.Message;
					return false;
				}

				if (entity.Status == (int)ProfileStatus.Active)
				{
					errorMessage = "Profile is already active";
					return false;
				}

				var mapProfile = new TblCorporateProfile
				{
					Id = Guid.NewGuid(),
					Title = profile.Title,
					CorporateCustomerId = entity.Id,
					Username = profile.UserName,
					Phone1 = profile.Phone1,
					Email = profile.Email1,
					FirstName = profile.FirstName,
					MiddleName = profile.MiddleName,
					ApprovalLimit = profile.ApprovalLimit,
					LastName = profile.LastName,
					Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword()),
					FullName = profile.FullName,
					Status = (int)ProfileStatus.Active,
					RegStage = 0,
					DefaultTranPin = profile.DefaultPin,
					PasswordExpiryDate = DateTime.Now.AddMinutes(5),
					ResetPinInitiated = (int)ProfileStatus.Active,
					DateCompleted = DateTime.Now
				};

				if (Enum.TryParse(entity.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
				{
					if (_authType == AuthorizationType.Single_Signatory)
					{
					}
					else
					{
						mapProfile.CorporateRole = profile.CorporateRoleId;
					}
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Active)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = " Approved Corporate Customer Account",
					TimeStamp = DateTime.Now
				};

				// UnitOfWork.Complete();
				entity.Status = (int)ProfileStatus.Active;
				entity.AddedBy = UserName;
				profile.IsTreated = (int)ProfileStatus.Active;
				entity.DateAdded = DateTime.Now;
				profile.CorporateCustomerId = mapProfile.Id;
				entity.Sn = 0;
				mapProfile.RegStage = 0;

				UnitOfWork.CorporateCustomerRepo.Add(entity);
				UnitOfWork.CorporateProfileRepo.Add(mapProfile);
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				var password = Encryption.DecriptPassword(mapProfile.Password);
				var path = Path.Combine(Template.CustomerProfileOnbording);
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.UserOnboardingMailWithCredentialMail(mapProfile.Email, mapProfile.FullName, mapProfile.Username, password, path)));

				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Update).Replace("_", " "))
			{
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
				if (entity == null)
				{
					errorMessage = "Invalid Corporate Customer Id";
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
					NewFieldValue = $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
						$"Authorization Type: {profile.AuthorizationType.Replace("_", " ")}, Default Account Name: {profile.DefaultAccountName}, " +
						$"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Update. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.CompanyName = profile.CompanyName;
				entity.CustomerId = profile.CustomerId;
				entity.Email1 = profile.Email1;
				entity.DefaultAccountName = profile.DefaultAccountName;
				entity.DefaultAccountNumber = profile.DefaultAccountNumber;
				entity.AuthorizationType = profile.AuthorizationType;

				var userStatus = UnitOfWork.CorporateCustomerRepo.CheckDuplicate(entity, true);
				if (userStatus.IsDuplicate != "02")
				{
					errorMessage = userStatus.Message;
					return false;
				}
				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
				profile.IsTreated = (int)ProfileStatus.Active;
				entity.Status = originalStatus;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				profile.Reasons = payload.Reason;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Change_Account_Signatory).Replace("_", " "))
			{
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
				if (entity == null)
				{
					errorMessage = "Invalid Corporate Customer Id";
					return false;
				}
				var profileEntity = UnitOfWork.CorporateProfileRepo.GetCorporateProfiles(entity.Id);
				if (!profileEntity.Any())
				{
					errorMessage = "no corporate profile is associated to this  Corporate Customer";
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
					NewFieldValue = $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
						$"Authorization Type: {profile.AuthorizationType.Replace("_", " ")}, Default Account Name: {profile.DefaultAccountName}, " +
						$"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Change of Corporate Customer Signatory. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};


				// check if the change is from single to multiple 

				if (entity.AuthorizationType == nameof(AuthorizationType.Multiple_Signatory) && profile.AuthorizationType == nameof(AuthorizationType.Single_Signatory))
				{
					foreach (var user in profileEntity)
					{
						if (user.Id != profile.CorporateProfileId)
						{
							user.Status = (int)ProfileStatus.Deactivated;
							UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(user);
						}
					}
				}
				else
				{
					var updatedProfile = profileEntity.FirstOrDefault();
					var originalProfileStatus = updatedProfile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
					updatedProfile.Status = originalProfileStatus;
					updatedProfile.CorporateRole = profile.CorporateRoleId;
					UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(updatedProfile);
				}
				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
				entity.PreviouseAuthorizationType = entity.AuthorizationType;
				entity.AuthorizationType = profile.AuthorizationType;
				profile.IsTreated = (int)ProfileStatus.Active;
				entity.Status = originalStatus;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				profile.Reasons = payload.Reason;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
			{
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
				if (entity == null)
				{
					errorMessage = "Invalid Corporate Customer Id";
					return false;
				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {nameof(ProfileStatus.Active)}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Role Update. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.MinAccountLimit = profile.MinAccountLimit;
				entity.MaxAccountLimit = profile.MaxAccountLimit;
				entity.SingleTransDailyLimit = profile.SingleTransDailyLimit;
				entity.BulkTransDailyLimit = profile.BulkTransDailyLimit;
				entity.AuthenticationLimit = profile.AuthenticationLimit;

				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
				entity.Status = originalStatus;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				profile.Reasons = payload.Reason;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Deactivate).Replace("_", " "))
			{
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
				if (entity == null)
				{
					errorMessage = "Invalid Corporate Customer Id";
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Deactivation. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.Status = (int)ProfileStatus.Deactivated;
				profile.Status = (int)ProfileStatus.Deactivated;
				profile.IsTreated = (int)ProfileStatus.Active;
				//entity.ReasonsForDeactivation = profile.Reasons;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
			{
				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
				if (entity == null)
				{
					errorMessage = "Invalid Corporate Customer Id";
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
					PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Reactivation. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.Status = (int)ProfileStatus.Active;
				profile.Status = (int)ProfileStatus.Active;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				profile.Reasons = "";
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Update_Corporate_short_Name).Replace("_", " "))
			{

				var notificationList = new List<EmailNotification>();

				var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
				if (entity == null)
				{
					errorMessage = "Invalid Corporate Customer Id";
					return false;
				}
				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"CoporateShortName: {entity.CorporateShortName}, Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId},Status: {nameof(ProfileStatus.Active)}",
					PreviousFieldValue = $"CoporateShortName: {entity.CorporateShortName} ,Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Corporate Short Name Update. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				var getCorporateProfiles = UnitOfWork.CorporateProfileRepo.GetProfileByCorporateCustomerId(entity.Id);
				if (!getCorporateProfiles.Any())
				{
					errorMessage = "No profiles associated with this corporate customer found";
					return false;
				}

				foreach (var customer in getCorporateProfiles)
				{
					var mailNotification = new EmailNotification();
					string[] oldUserName = customer.Username.Split('.');
					if (oldUserName.GetType() == typeof(string[]))
					{
						if (oldUserName.Length > 1)
						{
							var newUserName = $"{profile.CorporateShortName.ToLower().Trim()}.{oldUserName[1]}";
							customer.Username = newUserName;
							customer.Status = (int)ProfileStatus.Active;
							UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(customer);
							mailNotification.Email = customer.Email;
							mailNotification.UserName = newUserName;
							mailNotification.FullName = $"{customer.FullName}";
							notificationList.Add(mailNotification);
						}
						else
						{
							var userName = $"{profile.CorporateShortName.ToLower().Trim()}.{customer.Username.Trim()}";
							customer.Username = userName;
							customer.Status = (int)ProfileStatus.Active;
							UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(customer);
							mailNotification.Email = customer.Email;
							mailNotification.UserName = userName;
							mailNotification.FullName = $"{customer.FullName}";
							notificationList.Add(mailNotification);
						}
					}
				}

				entity.Status = (int)ProfileStatus.Active;
				entity.CorporateShortName = profile.CorporateShortName.ToLower().Trim();
				profile.Status = (int)ProfileStatus.Active;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				profile.Reasons = "";
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				var path = Path.Combine(Template.ResendUserName);
				foreach (var mailInfo in notificationList)
				{
					ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.ResendCustomerUserNameMail(mailInfo.Email, mailInfo.FullName, mailInfo.UserName, path)));
				}
				errorMessage = "";
				return true;
			}

			errorMessage = "Unknow Request";
			return false;
		}
		private bool RequestApproval(TblTempCorporateCustomer entity, SimpleAction payload, out string errorMessage)
		{
			var emailNotification = new EmailNotification
			{
				CompanyName = entity.CompanyName,
				CustomerId = entity.CustomerId,
				Action = entity.Action,
				MinAccountLimit = entity.MinAccountLimit,
				MaxAccountLimit = entity.MaxAccountLimit,
				SingleTransDailyLimit = entity.SingleTransDailyLimit,
				BulkTransDailyLimit = entity.BulkTransDailyLimit,
				ApprovalLimit = entity.ApprovalLimit
			};

			if (entity.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Company Email: {entity.Email1}, CustomerId: {entity.CustomerId}, Account Number: {entity.DefaultAccountNumber}, Account Name: {entity.DefaultAccountName}, AuthorizationType: {entity.AuthorizationType}, Phone Number: {entity.PhoneNumber}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Create Corporate Customer by bank admin",
					TimeStamp = DateTime.Now
				};

				//email notification
				entity.Status = (int)ProfileStatus.Pending;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity, emailNotification);
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Change_Account_Signatory).Replace("_", " "))
			{

				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
						$"Authorization Type: {profile.AuthorizationType.Replace("_", " ")}, Default Account Name: {profile.DefaultAccountName}, " +
						$"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Modified Corporate Customer Info By Bank Admin",
					TimeStamp = DateTime.Now
				};

				//update status

				var changeNotification = new EmailNotification
				{
					CompanyName = entity.CompanyName,
					CustomerId = entity.CustomerId,
					Action = entity.Action,
					MinAccountLimit = entity.MinAccountLimit,
					MaxAccountLimit = entity.MaxAccountLimit,
					SingleTransDailyLimit = entity.SingleTransDailyLimit,
					BulkTransDailyLimit = entity.BulkTransDailyLimit,
					ApprovalLimit = entity.ApprovalLimit
				};

				entity.Status = (int)ProfileStatus.Pending;
				profile.Status = (int)ProfileStatus.Pending;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity, changeNotification);
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
			{

				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
						$"Authorization Type: {profile.AuthorizationType.Replace("_", " ")}, Default Account Name: {profile.DefaultAccountName}, " +
						$"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {status}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Modified Corporate Customer Info By Bank Admin",
					TimeStamp = DateTime.Now
				};

				//update status
				entity.Status = (int)ProfileStatus.Pending;
				profile.Status = (int)ProfileStatus.Pending;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity, emailNotification);
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
			{
				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);

				if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
				{
					errorMessage = "Profile wasn't Decline or modified initially";
					return false;
				}

				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
						$"Maximum Account Limit: {profile.MaxAccountLimit}, Minimum Account Limit: {profile.MinAccountLimit}, Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Updated Account Limit of Corporate Customer",
					TimeStamp = DateTime.Now
				};

				//update status
				var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Pending;

				entity.Status = (int)ProfileStatus.Pending;
				profile.Status = originalStatus;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity, emailNotification);
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Corporate_short_Name).Replace("_", " "))
			{
				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
				if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
				{
					errorMessage = "Profile wasn't Decline or modified initially";
					return false;
				}

				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, Status: {nameof(ProfileStatus.Modified)}, Corporate Short Name {entity.CorporateShortName}",
					PreviousFieldValue = $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Update Corporate shot Name",
					TimeStamp = DateTime.Now
				};

				//update status
				var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Pending;
				entity.Status = (int)ProfileStatus.Pending;
				profile.Status = originalStatus;
				//profile.CorporateShortName = entity.CorporateShortName;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			errorMessage = "invalid Request";
			return false;
		}
		private bool DeclineRequest(TblTempCorporateCustomer entity, AppActionDto payload, out string errorMessage)
		{
			var initiatorProfile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);

			var emailNotification = new EmailNotification
			{
				CompanyName = entity.CompanyName,
				CustomerId = entity.CustomerId,
				Email = entity.CorporateEmail,
				AccountName = entity.DefaultAccountName,
				AccountNumber = entity.DefaultAccountNumber,
				Action = entity.Action,
				MinAccountLimit = entity.MinAccountLimit,
				MaxAccountLimit = entity.MaxAccountLimit,
				SingleTransDailyLimit = entity.SingleTransDailyLimit,
				BulkTransDailyLimit = entity.BulkTransDailyLimit,
				ApprovalLimit = entity.ApprovalLimit,
				Reason = payload.Reason
			};

			if (entity.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Approval for new Bank Profile. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};


				//update status
				entity.Status = (int)ProfileStatus.Declined;
				entity.IsTreated = (int)ProfileStatus.Declined;
				entity.Reasons = payload.Reason;
				entity.ApprovedId = BankProfile.Id;
				entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				entity.ApprovalUsername = UserName;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
				errorMessage = "Decline Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
			{

				var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
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
					NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
						$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
						$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Approval to Update Corporate Customer Information. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};
				var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
				entity.Status = (int)ProfileStatus.Declined;
				profile.Status = originalStatus;
				entity.IsTreated = (int)ProfileStatus.Declined;
				entity.Reasons = payload.Reason;
				entity.ApprovedId = BankProfile.Id;
				entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
				UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();

				notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
				errorMessage = "Decline Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
			{
				if (entity.CorporateCustomerId != null)
				{
					var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
					if (profile == null)
					{
						errorMessage = "Invalid Corporate Customer id";
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
						NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
													 $"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {nameof(ProfileStatus.Modified)}",

						PreviousFieldValue = "",
						TransactionId = "",
						UserId = BankProfile.Id,
						Username = UserName,
						Description = $"Decline Request for Bank Profile Role Change. Action was carried out by a Bank user",
						TimeStamp = DateTime.Now
					};

					//update status
					//notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
					var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
					entity.Status = (int)ProfileStatus.Declined;
					profile.Status = originalStatus;
					entity.IsTreated = (int)ProfileStatus.Declined;
					entity.Reasons = payload.Reason;
					entity.ApprovedId = BankProfile.Id;
					entity.ApprovalUsername = UserName;
					entity.ActionResponseDate = DateTime.Now;
					UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
					UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.Complete();
					notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
					errorMessage = "Request Decline Was Successful";
					return true;
				}
				errorMessage = "Invalid Corporate Customer id";
				return false;
			}

			if (entity.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
			{
				if (entity.CorporateCustomerId != null)
				{
					var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
					if (profile == null)
					{
						errorMessage = "Invalid Corporate Customer Id";
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
						NewFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
													 $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
													 $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
						PreviousFieldValue = $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
																$"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
																$"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
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
					entity.ApprovedId = BankProfile.Id;
					entity.ApprovalUsername = UserName;
					entity.ActionResponseDate = DateTime.Now;
					UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
					UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.Complete();
					notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
					errorMessage = "Decline Request Was Successful";
					return true;
				}
				errorMessage = "Invalid Corporate Customer Id is Require";
				return false;
			}

			if (entity.Action == nameof(TempTableAction.Update_Corporate_short_Name).Replace("_", " "))
			{
				if (entity.CorporateCustomerId != null)
				{
					var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
					if (profile == null)
					{
						errorMessage = "Invalid Corporate Customer Id";
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
						NewFieldValue = $"Corporate Short Name:{entity.CorporateShortName} ,Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, Status: {nameof(ProfileStatus.Deactivated)}",
						PreviousFieldValue = $"Corporate Short Name:{entity.CorporateShortName}, Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, Status: {status}",
						TransactionId = "",
						UserId = BankProfile.Id,
						Username = UserName,
						Description = $"Decline Request for corporate short name update. Action was carried out by a Bank user",
						TimeStamp = DateTime.Now
					};
					//update status
					var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
					entity.Status = (int)ProfileStatus.Declined;
					profile.Status = originalStatus;
					entity.IsTreated = (int)ProfileStatus.Declined;
					entity.Reasons = payload.Reason;
					entity.ApprovedId = BankProfile.Id;
					entity.ApprovalUsername = UserName;
					entity.ActionResponseDate = DateTime.Now;
					UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
					UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
					UnitOfWork.AuditTrialRepo.Add(auditTrail);
					UnitOfWork.Complete();
					notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile, emailNotification);
					errorMessage = "Decline Request Was Successful";
					return true;
				}
				errorMessage = "Invalid Corporate Customer Id is Require";
				return false;
			}

			if (entity.Action == nameof(TempTableAction.Change_Account_Signatory).Replace("_", " "))
			{
				if (entity.CorporateCustomerId != null)
				{
					var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
					if (profile == null)
					{
						errorMessage = "Invalid Corporate Customer Id";
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
						NewFieldValue = $"Corporate Short Name:{entity.CorporateShortName} ,Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, Status: {nameof(ProfileStatus.Deactivated)}",
						PreviousFieldValue = $"Corporate Short Name:{entity.CorporateShortName}, Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, Status: {status}",
						TransactionId = "",
						UserId = BankProfile.Id,
						Username = UserName,
						Description = $"Decline Request for Change Account Signatory. Action was carried out by a Bank user",
						TimeStamp = DateTime.Now
					};

					//update status
					var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
					entity.Status = (int)ProfileStatus.Declined;
					profile.Status = originalStatus;
					entity.IsTreated = (int)ProfileStatus.Declined;
					entity.Reasons = payload.Reason;
					entity.ApprovedId = BankProfile.Id;
					entity.ApprovalUsername = UserName;
					entity.ActionResponseDate = DateTime.Now;
					UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
					UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
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
}
