using System;
using System.Linq;
using CIB.BankAdmin.Utils;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.BankAdminProfile.Dto;
using CIB.Core.Modules.BankAdminProfile.Validation;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CIB.BankAdmin.Controllers
{
	[ApiController]
	[Route("api/BankAdmin/v1/[controller]")]
	public class BankProfileController : BaseAPIController
	{
		private readonly IEmailService _emailService;
		private readonly ILogger<BankProfileController> _logger;
		protected readonly INotificationService notify;
		public BankProfileController(INotificationService notify, ILogger<BankProfileController> _logger, IUnitOfWork unitOfWork, AutoMapper.IMapper mapper, IHttpContextAccessor accessor, IEmailService emailService, IAuthenticationService authService) : base(mapper, unitOfWork, accessor, authService)
		{
			this._emailService = emailService;
			this._logger = _logger;
			this.notify = notify;
		}

		[HttpGet("GetAllProfiles")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ListResponseDTO<BankAdminProfileResponse>> GetAllBankAdminProfiles()
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out var errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewBankAdminProfile))
				{
					return BadRequest("Aunthorized Access");
				}

				var corporateProfiles = UnitOfWork.BankProfileRepo.GetAllBankAdminProfiles().ToList();
				if (corporateProfiles.Count == 0)
				{
					return StatusCode(204);
				}
				return Ok(new ListResponseDTO<BankAdminProfileResponse>(_data: corporateProfiles, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return Ok(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("CreateProfile")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> CreateBankAdminProfile(GenericRequestDto model)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!IsUserActive(out var errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<CreateBankAdminProfileDTO>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new CreateBankAdminProfileDTO
				{
					Username = requestData.Username,
					PhoneNumber = requestData.PhoneNumber,
					Email = requestData.Email,
					FirstName = requestData.FirstName,
					MiddleName = requestData.MiddleName,
					LastName = requestData.LastName,
					UserRoleId = requestData.UserRoleId,
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					IPAddress = Encryption.DecryptStrings(model.IPAddress)
				};

				var validator = new CreateBankAdminProfileValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					LogFormater<BankProfileController>.Error(_logger, "CreateProfile", $"VALIDATION ERROR : {results}", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(BankProfile.Username));
					return UnprocessableEntity(new ValidatorResponse(_data: new object(), _success: false, _validationResult: results.Errors));
				}
				var roleName = UnitOfWork.RoleRepo.GetRoleName(payload.UserRoleId);
				//var checkDuplicate
				payload.Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword());
				var mapProfile = Mapper.Map<TblBankProfile>(payload);
				mapProfile.Phone = payload.PhoneNumber;
				mapProfile.Status = (int)ProfileStatus.Pending;
				mapProfile.RegStage = (int)ProfileStatus.Pending;
				mapProfile.UserRoles = payload.UserRoleId;

				var result = UnitOfWork.BankProfileRepo.CheckDuplicate(mapProfile);
				if (result.IsDuplicate)
				{
					return StatusCode(400, result.Message);
				}


				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"First Name: {mapProfile.FirstName}, Last Name: {mapProfile.LastName}, Username: {mapProfile.Username}, Email Address:  {mapProfile.Email}, " +
					$"Middle Name: {mapProfile.MiddleName}, Phone Number: {mapProfile.Phone}, Role: {roleName},Status: {ProfileStatus.Pending}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Created a new Bank User",
					TimeStamp = DateTime.Now
				};

				var mapTempProfile = Mapper.Map<TblTempBankProfile>(mapProfile);
				var middleName = payload.MiddleName == null ? "" : payload.MiddleName.Trim().ToLower();
				mapProfile.FullName = payload.FirstName.Trim().ToLower() + " " + middleName + " " + payload.LastName.Trim().ToLower();
				mapTempProfile.Sn = 0;
				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.IsTreated = (int)ProfileStatus.Pending;
				mapTempProfile.Status = (int)ProfileStatus.Modified;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.Action = nameof(TempTableAction.Create).Replace("_", " ");
				mapTempProfile.UserRoles = payload.UserRoleId;


				var tempResult = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicate(mapProfile);
				if (tempResult.IsDuplicate)
				{
					return StatusCode(400, tempResult.Message);
				}
				var responseObject = Mapper.Map<BankAdminProfileResponse>(mapProfile);
				responseObject.PhoneNumber = mapProfile.Phone;
				responseObject.UserRoleName = roleName;
				UnitOfWork.TemBankAdminProfileRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifyBankAdminAuthorizer(mapTempProfile, true, "BankProfile Onboarded");
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: responseObject, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpGet("GetProfileById")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> GetBankAdminProfile(string id)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				// string errormsg = string.Empty;

				if (!IsUserActive(out string errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewBankAdminProfile))
				{
					return BadRequest("UnAuthorized Access");
				}

				var bankId = Encryption.DecryptGuid(id);
				var adminProfile = UnitOfWork.BankProfileRepo.GetBankAdminProfileById(bankId);
				if (adminProfile == null)
				{
					return BadRequest("Invalid id. Admin Profile not found");
				}
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: adminProfile, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return Ok(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("UpdateProfile")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> UpdateBankAdminProfile(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
				{
					return BadRequest("UnAuthorized Access");
				}
				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<UpdateBankAdminProfileDTO>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new UpdateBankAdminProfileDTO
				{
					Id = requestData.Id,
					Username = requestData.Username,
					PhoneNumber = requestData.PhoneNumber,
					Email = requestData.Email,
					FirstName = requestData.FirstName,
					MiddleName = requestData.MiddleName,
					LastName = requestData.LastName,
					UserRoleId = requestData.UserRoleId,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var validator = new UpdateBankAdminProfileValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					LogFormater<BankProfileController>.Error(_logger, "UpdateProfile", $"VALIDATION ERROR : {results}", JsonConvert.SerializeObject(payload), JsonConvert.SerializeObject(BankProfile.Username));
					return UnprocessableEntity(new ValidatorResponse(_data: new object(), _success: false, _validationResult: results.Errors));
				}

				//get profile by username
				var profileData = UnitOfWork.BankProfileRepo.GetByIdAsync(payload.Id);
				if (profileData == null)
				{
					return BadRequest("Invalid ID");
				}

				if (profileData.Status == (int)ProfileStatus.Deactivated)
				{
					return BadRequest("Action is not allow becouse the profile is deactivated");
				}

				if (profileData.Status == (int)ProfileStatus.Pending)
				{

					return BadRequest("There is a pending request awaiting Approval");
				}

				var checkDuplicateRequest = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicateRequest(profileData, nameof(TempTableAction.Update).Replace("_", " "));
				if (checkDuplicateRequest.Count != 0)
				{
					return BadRequest("There is an on going modification on this profile");
				}

				var status = (ProfileStatus)profileData.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"First Name: {payload.FirstName}, Last Name: {payload.LastName}, Username: {payload.Username}, Email Address:  {payload.Email}, " +
					$"Middle Name: {payload.MiddleName}, Phone Number: {payload.PhoneNumber},Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"First Name: {profileData.FirstName}, Last Name: {profileData.LastName}, Username: {profileData.Username}, Email Address:  {profileData.Email}, " +
					$"Middle Name: {profileData.MiddleName}, Phone Number: {profileData.Phone},Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Updated a new Bank User",
					TimeStamp = DateTime.Now
				};

				//update existing info


				var mapTempProfile = Mapper.Map<TblTempBankProfile>(profileData);

				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.Sn = 0;
				mapTempProfile.BankProfileId = profileData.Id;
				mapTempProfile.UserRoles = profileData.UserRoles;
				mapTempProfile.IsTreated = (int)ProfileStatus.Pending;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.FirstName = payload.FirstName;
				mapTempProfile.LastName = payload.LastName;
				mapTempProfile.MiddleName = payload.MiddleName;
				mapTempProfile.Email = payload.Email;
				mapTempProfile.Phone = payload.PhoneNumber;

				var check = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicate(mapTempProfile, true);
				if (check.IsDuplicate)
				{
					return BadRequest(check.Message);
				}

				var mapProfile = Mapper.Map<TblBankProfile>(mapTempProfile);
				mapProfile.Id = profileData.Id;
				var checkStatus = UnitOfWork.BankProfileRepo.CheckDuplicates(mapProfile, true);
				if (checkStatus.IsDuplicate)
				{
					return BadRequest(checkStatus.Message);
				}

				var middleName = payload.MiddleName == null ? "" : payload.MiddleName.Trim().ToLower();
				mapTempProfile.FullName = payload.FirstName.Trim().ToLower() + " " + middleName + " " + payload.LastName.Trim().ToLower();
				mapTempProfile.PreviousStatus = profileData.Status;
				mapTempProfile.Status = (int)ProfileStatus.Modified;
				mapTempProfile.Action = nameof(TempTableAction.Update_Phone_Number).Replace("_", " ");

				var originalStatus = profileData.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
				profileData.Status = originalStatus;
				UnitOfWork.BankProfileRepo.UpdateBankProfile(profileData);
				UnitOfWork.TemBankAdminProfileRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(profileData);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("RequestProfileApproval")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> RequestProfileApproval(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
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

				var payload = new SimpleAction
				{
					Id = requestData.Id,
					Reason = requestData.Reason,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};
				var entity = UnitOfWork.TemBankAdminProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}

				if (entity.Status == 1) return BadRequest("Profile was not declined or modified initially");

				if (!RequestApproval(entity, payload, out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}


				if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
				{
					return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: new BankAdminProfileResponse(), success: true, _message: Message.Success));
				}

				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
				if (profile == null)
				{
					return BadRequest("Invalid Profile Id");
				}

				var mapResponse = Mapper.Map<BankAdminProfileResponse>(profile);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("ApproveProfile")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> ApproveProfile(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminAuthorizer(UserRoleId))
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


				var payload = new SimpleAction
				{
					Id = requestData.Id,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var entity = UnitOfWork.TemBankAdminProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}

				if (entity.Status == 1) return BadRequest("Profile is already active");


				if (!ApprovedRequest(entity, payload, out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}

				if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
				{
					return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: new BankAdminProfileResponse(), success: true, _message: Message.Success));
				}

				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
				if (profile == null)
				{
					return BadRequest("Invalid Profile Id");
				}


				// var status = (ProfileStatus)entity.Status;
				// var auditTrail = new TblAuditTrail
				// {
				//   Id = Guid.NewGuid(),
				//   ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
				//   Ipaddress = payload.IPAddress,
				//   Macaddress = payload.MACAddress,
				//   HostName = payload.HostName,
				//   ClientStaffIpaddress = payload.ClientStaffIPAddress,
				//   NewFieldValue =  $"Profile Status: {nameof(ProfileStatus.Active)}, Username: {entity.Username}First Name: {entity.FirstName}, Last Name: {entity.LastName}",
				//   PreviousFieldValue = $"Profile Status: {status},Username: {entity.Username},First Name: {entity.FirstName}, Last Name: {entity.LastName}",
				//   TransactionId = "",
				//   UserId = BankProfile.Id,
				//   Username = UserName,
				//   Description = "Approved Bank User Profile",
				//   TimeStamp = DateTime.Now
				// };
				// //update status
				// entity.Status = (int)ProfileStatus.Active;
				// int? regStage = entity.RegStage;
				// if (regStage == 0)
				// {
				//   string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
				//   _emailService.SendEmail(EmailTemplate.LoginCredentialAdminMail(entity.Email, fullName, entity.Username, "", ""));
				// }
				// entity.RegStage = 1;
				// UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				// UnitOfWork.AuditTrialRepo.Add(auditTrail);
				// UnitOfWork.Complete();
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(profile);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("ReActivateProfile")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> ReActivateProfile(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
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

				var payload = new SimpleAction
				{
					Id = requestData.Id,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				if (string.IsNullOrEmpty(payload.Id.ToString()))
				{
					return BadRequest("Invalid format");
				}

				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}
				if (entity.Status != -1) return BadRequest("Profile was not deactivated");

				var checkDuplicateRequest = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicateRequest(entity, nameof(TempTableAction.Reactivate).Replace("_", " "));
				if (checkDuplicateRequest.Count != 0)
				{
					return BadRequest("There is a pending request awaiting Approval");
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
					NewFieldValue = $"Profile Status: {nameof(ProfileStatus.Active)}, Username: {entity.Username}First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					PreviousFieldValue = $"Profile Status: {status},Username: {entity.Username},First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Reactivated Bank User Profile",
					TimeStamp = DateTime.Now
				};

				//update status
				var mapTempProfile = Mapper.Map<TblTempBankProfile>(entity);

				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.Sn = 0;
				mapTempProfile.BankProfileId = entity.Id;
				mapTempProfile.IsTreated = 0;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.PreviousStatus = entity.Status;
				mapTempProfile.Status = (int)ProfileStatus.Pending;
				//entity.Status = (int)ProfileStatus.Modified;
				mapTempProfile.Action = nameof(TempTableAction.Reactivate).Replace("_", " ");
				mapTempProfile.Reasons = payload.Reason;
				//entity.Status = (int)ProfileStatus.Modified;
				//entity.NoOfWrongAttempts = 0;
				//entity.ReasonsForDeactivation = string.Empty;
				// UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.TemBankAdminProfileRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				// entity.Status = (int)ProfileStatus.Active;
				// entity.ReasonsForDeactivation = string.Empty;
				// entity.NoOfWrongAttempts = 0;
				// UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				// UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(entity);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("DeclineProfile")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> DeclineProfile(GenericRequestDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminAuthorizer(UserRoleId))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(model.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<DeclineBankAdminProfileDTO>(Encryption.DecryptStrings(model.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}

				var payload = new DeclineBankAdminProfileDTO
				{
					Id = requestData.Id,
					Reason = requestData.Reason,
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var validator = new DeclineBankAdminProfileValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new object(), _success: false, _validationResult: results.Errors));
				}

				var entity = UnitOfWork.TemBankAdminProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}

				//if (entity.Status != 0) return BadRequest("Profile is not awaiting approval and cannot be declined");

				if (!DeclineRequest(entity, payload, out string errorMessage))
				{
					return StatusCode(400, errorMessage);
				}

				if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
				{
					return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: new BankAdminProfileResponse(), success: true, _message: Message.Success));
				}

				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);


				// var status = (ProfileStatus)entity.Status;
				// var auditTrail = new TblAuditTrail
				// {
				//   Id = Guid.NewGuid(),
				//   ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
				//   Ipaddress = payload.IPAddress,
				//   Macaddress = payload.MACAddress,
				//   HostName = payload.HostName,
				//   ClientStaffIpaddress = payload.ClientStaffIPAddress,
				//   NewFieldValue = $"Profile Status: {nameof(ProfileStatus.Declined)}, Username: {entity.Username}, First Name: {entity.FirstName},Last Name: {entity.LastName}, Reason: {payload.Reason}",
				//   PreviousFieldValue = $"Profile Status: {status}, Username: {entity.Username},First Name: {entity.FirstName}, Last Name: {entity.LastName}, Reason: {entity.ReasonsForDeclining}",
				//   TransactionId = "",
				//   UserId = BankProfile.Id,
				//   Username = UserName,
				//   Description = "Declined Bank User Profile",
				//   TimeStamp = DateTime.Now
				// };
				// //update existing info
				// entity.Status = (int)ProfileStatus.Declined;
				// entity.ReasonsForDeclining = payload.Reason;
				// UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				// UnitOfWork.Complete();
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(profile);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("DeactivateProfile")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> DeactivateProfile(AppAction model)
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

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeactivateBankAdminProfile))
				{
					return BadRequest("UnAuthorized Access");
				}

				var payload = new DeactivateBankAdminProfileDTO
				{
					Id = Encryption.DecryptGuid(model.Id),
					Reason = Encryption.DecryptStrings(model.Reason),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};
				var validator = new DeactivateBankAdminProfileValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new object(), _success: false, _validationResult: results.Errors));
				}

				//get profile by id
				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}
				if (entity.Status == -1) return BadRequest("Profile is already de-activated");



				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Profile Status: {nameof(ProfileStatus.Deactivated)}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}, Reason: {payload.Reason}",
					PreviousFieldValue = $"Profile Status: {status}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}, Reason: {entity.ReasonsForDeactivation}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Deactivated Bank User Profile",
					TimeStamp = DateTime.Now
				};
				//update existing info  

				var checkDuplicateRequest = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicateRequest(entity, nameof(TempTableAction.Deactivate).Replace("_", " "));
				if (checkDuplicateRequest.Count != 0)
				{
					return BadRequest("There is a pending request awaiting Approval");
				}
				var mapTempProfile = Mapper.Map<TblTempBankProfile>(entity);
				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.Sn = 0;
				mapTempProfile.BankProfileId = entity.Id;
				mapTempProfile.IsTreated = (int)ProfileStatus.Deactivated;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.PreviousStatus = entity.Status;
				mapTempProfile.Status = (int)ProfileStatus.Pending;
				mapTempProfile.Action = nameof(TempTableAction.Deactivate).Replace("_", " ");
				mapTempProfile.Reasons = payload.Reason;
				entity.Status = (int)ProfileStatus.Deactivated;
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.TemBankAdminProfileRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(entity);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("UpdateProfileUserRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> UpdateProfileUserRole(UpdateBankAdminProfileUserRole model)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateBankAdminUserRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				string errormsg = string.Empty;

				if (!IsUserActive(out errormsg))
				{
					return StatusCode(400, errormsg);
				}
				var payload = new UpdateBankAdminProfileUserRoleDTO
				{
					Id = Encryption.DecryptGuid(model.Id),
					RoleId = Encryption.DecryptGuid(model.RoleId),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				//get profile by id
				var validator = new UpdateBankAdminProfileUserRoleValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new object(), _success: false, _validationResult: results.Errors));
				}

				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}

				if (entity.Status == (int)ProfileStatus.Deactivated)
				{
					return BadRequest("Action is not allow becouse the profile is deactivated");
				}

				if (entity.Status == (int)ProfileStatus.Pending)
				{

					return BadRequest("There is a pending request awaiting Approval");
				}

				var checkDuplicateRequest = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicateRequest(entity, nameof(TempTableAction.Update_Role).Replace("_", " "));
				if (checkDuplicateRequest.Count != 0)
				{
					return BadRequest("There is a pending request awaiting Approval");
				}

				var userRole = UnitOfWork.RoleRepo.GetRoleName(payload.RoleId.ToString());
				var previousRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(entity.UserRoles));
				var status = (ProfileStatus)entity.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"User Role: {userRole}, Profile Status: {ProfileStatus.Modified}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					PreviousFieldValue = $"User Role: {previousRole?.RoleName}, Profile Status: {status}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Modified Bank User's Role",
					TimeStamp = DateTime.Now
				};
				//update existing info

				var mapTempProfile = Mapper.Map<TblTempBankProfile>(entity);

				mapTempProfile.Id = Guid.NewGuid();
				mapTempProfile.Sn = 0;
				mapTempProfile.BankProfileId = entity.Id;
				mapTempProfile.IsTreated = 0;
				mapTempProfile.InitiatorId = BankProfile.Id;
				mapTempProfile.InitiatorUsername = UserName;
				mapTempProfile.DateRequested = DateTime.Now;
				mapTempProfile.PreviousStatus = entity.Status;
				mapTempProfile.Status = (int)ProfileStatus.Modified;
				mapTempProfile.UserRoles = payload.RoleId.ToString();
				mapTempProfile.Action = nameof(TempTableAction.Update_Role).Replace("_", " ");

				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
				entity.Status = originalStatus;
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.TemBankAdminProfileRepo.Add(mapTempProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				//  make it require and approfval
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(entity);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{

				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPut("EnableProfileLoggedOut")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<BankAdminProfileResponse>> EnableProfileLoggedOut(SimpleActionDto model)
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

				if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.EnableLoggedOutBankAdminUser))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(model.Id))
				{
					return BadRequest("Invalid format");
				}
				var payload = new SimpleAction
				{
					Id = Encryption.DecryptGuid(model.Id),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};

				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync(payload.Id);
				if (entity == null)
				{
					return BadRequest("Invalid Id");
				}

				if (entity.Status == 1) return BadRequest("Profile is already active");

				if (!(entity.NoOfWrongAttempts >= 3) && entity.ReasonsForDeactivation != "Multiple incorrect login attempt")
				{
					return BadRequest("This profile can not be enabled by you");
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
					NewFieldValue = $"Profile Status: {nameof(ProfileStatus.Active)}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					PreviousFieldValue = $"Profile Status: {status}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Enabled loggedout Bank User Profile",
					TimeStamp = DateTime.Now
				};
				//update status
				entity.Status = (int)ProfileStatus.Active;
				entity.NoOfWrongAttempts = 0;
				entity.ReasonsForDeactivation = string.Empty;
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				var mapResponse = Mapper.Map<BankAdminProfileResponse>(entity);
				return Ok(new ResponseDTO<BankAdminProfileResponse>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpGet("PendingApproval")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ListResponseDTO<TblTempBankProfile>> GetPendingApproval()
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

				if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewBankAdminProfile))
				{
					return BadRequest("UnAuthorized Access");
				}

				var corporateProfile = UnitOfWork.TemBankAdminProfileRepo.GetBankProfilePendingApprovals((int)ProfileStatus.Pending);
				if (corporateProfile == null)
				{
					return BadRequest("Invalid id. Corporate Profile not found");
				}

				return Ok(new ListResponseDTO<TblTempBankProfile>(_data: corporateProfile, success: true, _message: Message.Success));
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
		private bool ApprovedRequest(TblTempBankProfile profile, SimpleAction payload, out string errorMessage)
		{
			var tblRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(profile.UserRoles));
			if (tblRole == null)
			{
				errorMessage = "Invalid role id";
				return false;
			}

			if (profile.Action == nameof(TempTableAction.Create).Replace("_", " "))
			{

				var Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword());
				var mapProfile = Mapper.Map<TblBankProfile>(profile);
				mapProfile.Password = Password;

				var userStatus = UnitOfWork.BankProfileRepo.CheckDuplicate(mapProfile, profile.BankProfileId);
				if (userStatus.IsDuplicate)
				{
					errorMessage = userStatus.Message;
					return false;
				}

				if (mapProfile.Status == (int)ProfileStatus.Active)
				{
					errorMessage = "Profile is already active";
					return false;
				}

				var status = (ProfileStatus)mapProfile.Status;
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Profile Status: {nameof(ProfileStatus.Active)}, Username: {mapProfile.Username}First Name: {mapProfile.FirstName}, Last Name: {mapProfile.LastName}",
					PreviousFieldValue = $"Profile Status: {status},Username: {mapProfile.Username},First Name: {mapProfile.FirstName}, Last Name: {mapProfile.LastName}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Approved Bank User Profile",
					TimeStamp = DateTime.Now
				};
				//update status
				if (mapProfile.RegStage == 0)
				{
					//string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
					_emailService.SendEmail(EmailTemplate.LoginCredentialAdminMail(profile.Email, profile.FullName, profile.Username, "", ""));
				}
				mapProfile.RegStage = 1;
				mapProfile.Sn = 0;
				mapProfile.Status = (int)ProfileStatus.Active;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				mapProfile.DateStarted = profile.DateRequested;
				mapProfile.DateCompleted = DateTime.Now;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(profile);
				UnitOfWork.BankProfileRepo.Add(mapProfile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Update_Phone_Number).Replace("_", " "))
			{
				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)profile.BankProfileId);
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $" First Name: {profile.FirstName},Last Name: {profile.LastName}, Username: {profile.Username}, Email Address:  {profile.Email}, " +
					$"Middle Name: {profile.MiddleName}, Phone Number: {profile.Phone},Status: {nameof(ProfileStatus.Active)}",
					PreviousFieldValue = $"First Name: {entity.FirstName}, Status: {nameof(ProfileStatus.Active)}" +
					$"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email},Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone},",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Update. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.LastName = profile.LastName;
				entity.FirstName = profile.FirstName;
				entity.MiddleName = profile.MiddleName;
				entity.Email = profile.Email;
				entity.Phone = profile.Phone;

				var userStatus = UnitOfWork.BankProfileRepo.CheckDuplicate(entity, profile.BankProfileId);
				if (userStatus.IsDuplicate)
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

				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(profile);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = userStatus.Message;
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
			{
				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)profile.BankProfileId);
				var previousRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(entity.UserRoles));
				if (previousRole == null)
				{
					errorMessage = "Invalid role id";
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
					NewFieldValue = $"First Name: {profile.FirstName}, Last Name: {profile.LastName}, Username: {profile.Username}, Email Address:  {profile.Email}, " +
						$"Middle Name: {profile.MiddleName}, Phone Number: {profile.Phone}, Role: {tblRole?.RoleName},Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
						$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}, Role: {previousRole?.RoleName},Status: {nameof(ProfileStatus.Active)}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Role Update. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				var originalStatus = entity.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)ProfileStatus.Active;
				entity.UserRoles = profile.UserRoles;
				entity.Status = originalStatus;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				profile.Reasons = payload.Reason;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(profile);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Deactivate).Replace("_", " "))
			{
				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)profile.BankProfileId);
				if (entity == null)
				{
					errorMessage = "Invalid Bank Profile id";
					return false;
				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"First Name: {profile.FirstName},Last Name: {profile.LastName}, Username: {profile.Username}, Email Address:  {profile.Email}, " +
						$"Middle Name: {profile.MiddleName}, Phone Number: {profile.Phone}, Role: {tblRole?.RoleName}, Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
						$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone},Status: {nameof(ProfileStatus.Active)}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Deactivation. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};


				entity.Status = (int)ProfileStatus.Deactivated;
				entity.ReasonsForDeactivation = profile.Reasons;
				profile.Status = (int)ProfileStatus.Deactivated;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;

				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(profile);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			if (profile.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
			{
				var entity = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)profile.BankProfileId);
				if (entity == null)
				{
					errorMessage = "Invalid Bank Profile id";
					return false;
				}
				var previousRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(entity.UserRoles));
				if (previousRole == null)
				{
					errorMessage = "Invalid role id";
					return false;
				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"First Name: {profile.FirstName}, Last Name: {profile.LastName}, Username: {profile.Username}, Email Address:  {profile.Email}, " +
						$"Middle Name: {profile.MiddleName}, Phone Number: {profile.Phone}, Role: {tblRole?.RoleName},Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
					$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}, Role: {previousRole?.RoleName},Status: {nameof(ProfileStatus.Active)}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Approved Bank Profile Reactivation. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.Status = (int)ProfileStatus.Active;
				entity.ReasonsForDeactivation = "";
				profile.Status = (int)ProfileStatus.Active;
				profile.IsTreated = (int)ProfileStatus.Active;
				profile.ApprovedId = BankProfile.Id;
				profile.ApprovalUsername = UserName;
				profile.ActionResponseDate = DateTime.Now;
				entity.NoOfWrongAttempts = 0;
				profile.Reasons = "";
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(profile);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				errorMessage = "";
				return true;
			}

			errorMessage = "Unknow Request";
			return false;
		}
		private bool RequestApproval(TblTempBankProfile entity, SimpleAction payload, out string errorMessage)
		{
			var tblRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(entity.UserRoles));
			if (tblRole == null)
			{
				errorMessage = "Invalid role id";
				return false;
			}
			var notifyInfo = new EmailNotification
			{
				FirstName = entity.FirstName,
				LastName = entity.LastName,
				MiddleName = entity.MiddleName,
				Email = entity.Email,
				PhoneNumber = entity.Phone,
				Role = tblRole.RoleName,
				Action = entity.Action
			};

			// var entity = UnitOfWork.TemBankAdminProfileRepo.GetBankProfilePendingApproval(profile,0);
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
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
					$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}, Role: {tblRole.RoleName},Status: {ProfileStatus.Pending}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Request Approval for new Created a new Bank User",
					TimeStamp = DateTime.Now
				};
				entity.Status = (int)ProfileStatus.Pending;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				//notify.NotifyBankAdminAuthorizer(entity,true, payload.Reason);

				notify.NotifySuperAdminBankAuthorizerForBankProfileApproval(notifyInfo);
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Phone_Number).Replace("_", " "))
			{
				// var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
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
					ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
				$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone},Status: {nameof(ProfileStatus.Modified)}",
					PreviousFieldValue = $"First Name: {profile.FirstName}, Last Name: {profile.LastName}, Username: {profile.Username}, Email Address:  {profile.Email}, " +
				$"Middle Name: {profile.MiddleName}, Phone Number: {profile.Phone},Status: {status}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Request Approval for Updating a new Bank User",
					TimeStamp = DateTime.Now
				};

				entity.Status = (int)ProfileStatus.Pending;
				profile.Status = (int)ProfileStatus.Pending;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				//notify.NotifyBankAdminAuthorizer(entity,true, payload.Reason);
				notify.NotifySuperAdminBankAuthorizerForBankProfileApproval(notifyInfo);
				errorMessage = "Request Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
			{
				// var tblRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(entity.UserRoles));
				// if (tblRole == null)
				// {
				//     errorMessage = "Invalid role id";
				//     return false;
				// }
				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
				var previousRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(profile.UserRoles));

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
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"User Role: {tblRole?.RoleName}, Profile Status: {ProfileStatus.Modified}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
					PreviousFieldValue = $"User Role: {previousRole?.RoleName}, Profile Status: {status}, Username: {profile.Username}, First Name: {profile.FirstName}, Last Name: {profile.LastName}",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = "Request Approval for Updating a Bank User Role",
					TimeStamp = DateTime.Now
				};

				//update status
				entity.Status = (int)ProfileStatus.Pending;
				profile.Status = (int)ProfileStatus.Pending;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				//notify.NotifyBankAdminAuthorizer(entity,true, payload.Reason);
				notify.NotifySuperAdminBankAuthorizerForBankProfileApproval(notifyInfo);
				errorMessage = "Request Approval Was Successful";
				return true;
			}
			errorMessage = "invalid Request";
			return false;
		}
		private bool DeclineRequest(TblTempBankProfile entity, DeclineBankAdminProfileDTO payload, out string errorMessage)
		{
			var initiatorProfile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);
			var tblRole = UnitOfWork.RoleRepo.GetByIdAsync(Guid.Parse(entity.UserRoles));
			if (tblRole == null)
			{
				errorMessage = "Invalid role id";
				return false;
			}

			var notifyInfo = new EmailNotification
			{
				FullName = entity.FullName,
				Email = entity.Email,
				PhoneNumber = entity.Phone,
				Role = tblRole.RoleName
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
					NewFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
					$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}, Role: {tblRole?.RoleName}, Status: {nameof(ProfileStatus.Pending)}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Approval for new Bank Profile. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				//update status
				//notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
				entity.Status = (int)ProfileStatus.Declined;
				entity.IsTreated = (int)ProfileStatus.Declined;
				entity.Reasons = payload.Reason;
				entity.ApprovedId = BankProfile.Id;
				entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				//notify.NotifyBankMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
				notify.NotifySuperAdminBankAuthorizerForBankProfileDecline(initiatorProfile, notifyInfo);
				errorMessage = "Decline Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Phone_Number).Replace("_", " "))
			{
				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
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
					NewFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
				$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone},Status: {nameof(ProfileStatus.Pending)}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Approval for bank Profile Update. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};
				//update status
				//notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
				var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)entity.PreviousStatus;

				entity.Status = (int)ProfileStatus.Declined;
				profile.Status = originalStatus;
				entity.IsTreated = (int)ProfileStatus.Declined;
				profile.ReasonsForDeclining = entity.Reasons;
				entity.Reasons = payload.Reason;
				entity.ApprovedId = BankProfile.Id;
				entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				//notify.NotifyBankMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
				notify.NotifySuperAdminBankAuthorizerForBankProfileDecline(initiatorProfile, notifyInfo);
				errorMessage = "Decline Approval Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Update_Role).Replace("_", " "))
			{
				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
				if (profile == null)
				{
					errorMessage = "Invalid Bank Profile  id";
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
					NewFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
					$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}, Role: {tblRole?.RoleName},Status: {nameof(ProfileStatus.Pending)}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Request for Bank Profile Role Change. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				var originalStatus = profile.Status == (int)ProfileStatus.Deactivated ? (int)ProfileStatus.Deactivated : (int)entity.PreviousStatus;
				entity.Status = (int)ProfileStatus.Declined;
				profile.Status = originalStatus;
				entity.IsTreated = (int)ProfileStatus.Declined;
				entity.Reasons = payload.Reason;
				entity.ApprovedId = BankProfile.Id;
				entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifySuperAdminBankAuthorizerForBankProfileDecline(initiatorProfile, notifyInfo);
				errorMessage = "Request Decline Was Successful";
				return true;
			}

			if (entity.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
			{
				var profile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.BankProfileId);
				if (profile == null)
				{
					errorMessage = "Invalid role id";
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
					NewFieldValue = $"First Name: {entity.FirstName},Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
					$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone},Status: {status}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = BankProfile.Id,
					Username = UserName,
					Description = $"Decline Request for Bank Profile Reactivation. Action was carried out by a Bank user",
					TimeStamp = DateTime.Now
				};

				entity.Status = (int)ProfileStatus.Declined;
				profile.ReasonsForDeactivation = entity.Reasons;
				entity.IsTreated = (int)ProfileStatus.Declined;
				entity.Reasons = payload.Reason;
				entity.ApprovedId = BankProfile.Id;
				entity.ApprovalUsername = UserName;
				entity.ActionResponseDate = DateTime.Now;
				UnitOfWork.TemBankAdminProfileRepo.UpdateTemBankAdminProfile(entity);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(profile);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				notify.NotifySuperAdminBankAuthorizerForBankProfileDecline(initiatorProfile, notifyInfo);
				//notify.NotifyBankMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
				errorMessage = "Decline Request Was Successful";
				return true;
			}

			errorMessage = "invalid Request";
			return false;
		}
	}
}