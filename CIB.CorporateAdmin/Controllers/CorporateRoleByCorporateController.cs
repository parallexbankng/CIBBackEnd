using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Modules.CorporateRole.Dto;
using CIB.Core.Modules.CorporateRole.Validation;
using CIB.Core.Modules.UserAccess.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class CorporateRoleByCorporateController : BaseAPIController
	{
		private readonly ILogger<CorporateRoleByCorporateController> _logger;
		public CorporateRoleByCorporateController(ILogger<CorporateRoleByCorporateController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
		{
			_logger = logger;
		}

		[HttpGet("GetRoles")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ListResponseDTO<TblCorporateRole>> GetCorporateRoles()
		{
			if (!IsAuthenticated)
			{
				return StatusCode(401, "User is not authenticated");
			}

			if (!IsUserActive(out string errormsg))
			{
				return StatusCode(400, errormsg);
			}

			if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateRole))
			{
				return BadRequest("UnAuthorized Access");
			}

			if (!IsUserActive(out string errorMessage))
			{
				return StatusCode(400, errorMessage);
			}

			var tblCorporateProfile = CorporateProfile;
			if (tblCorporateProfile.CorporateCustomerId == null)
			{
				return BadRequest("UnAuthorized Access");
			}

			var CorporateRoles = UnitOfWork.CorporateRoleRepo.GetAllCorporateRolesByCorporateId((Guid)tblCorporateProfile.CorporateCustomerId).ToList();
			if (CorporateRoles == null || CorporateRoles?.Count == 0)
			{
				return StatusCode(204);
			}
			return Ok(new ListResponseDTO<TblCorporateRole>(_data: CorporateRoles, success: true, _message: Message.Success));
		}

		[HttpGet("GetRole{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ResponseDTO<TblCorporateRole>> GetCorporateRole(string id)
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
				if (!string.IsNullOrEmpty(id))
				{
					return BadRequest("Invalid id");
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}
				var roleId = Encryption.DecryptGuid(id);
				var CorporateRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (CorporateRole == null)
				{
					return BadRequest("Invalid id. CorporateRole not found");
				}

				return Ok(new ResponseDTO<TblCorporateRole>(_data: CorporateRole, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return Ok(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}

		[HttpPost("CreateRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<TblCorporateRole> CreateCorporateRole(CreateCorporateRole model)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				var payload = new CreateCorporateRoleDto
				{
					CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
					ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
					RoleName = Encryption.DecryptStrings(model.RoleName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};

				var validator = new CreateCorporateRoleValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)payload.CorporateCustomerId);
				if (corporateCustomer == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
					Ipaddress = "",
					Macaddress = "",
					HostName = "",
					NewFieldValue = $"Company Name: {corporateCustomer.CompanyName}, Corporate Role: {payload.RoleName}, ApprovalLimit: {payload.ApprovalLimit}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = "Create Corporate Role"
				};
				var mapRole = Mapper.Map<TblCorporateRole>(payload);
				mapRole.Status = 0;
				mapRole.CorporateCustomerId = CorporateProfile.CorporateCustomerId;
				UnitOfWork.CorporateRoleRepo.Add(mapRole);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateRoleResponseDto>(_data: Mapper.Map<CorporateRoleResponseDto>(mapRole), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("UpdateRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<CorporateRoleResponseDto>> UpdateCorporateRole(UpdateCorporateRole model)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				var payload = new UpdateCorporateRoleDto
				{
					Id = Encryption.DecryptGuid(model.Id),
					CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
					ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
					RoleName = Encryption.DecryptStrings(model.RoleName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};

				//get data
				var _CorporateRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(payload.Id);
				if (_CorporateRole == null)
				{
					return BadRequest("Invalid Id.");
				}

				if (_CorporateRole.CorporateCustomerId == null)
				{
					return BadRequest("UnAuthorized Access");
				}
				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				var validator = new UpdateCorporateRoleValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)payload.CorporateCustomerId);
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
					Ipaddress = "",
					Macaddress = "",
					HostName = "",
					NewFieldValue = $"Company Name: {corporateCustomer.CompanyName}, Corporate Role: {payload.RoleName}, ApprovalLimit: {payload.ApprovalLimit}",
					PreviousFieldValue = $"Company Name: {corporateCustomer.CompanyName}, Corporate Role: {_CorporateRole.RoleName}, ApprovalLimit: {_CorporateRole.ApprovalLimit}",
					TransactionId = "",
					UserId = Guid.Parse(UserRoleId),
					Username = UserName,
					Description = $"Update Corporate Role. {payload.RoleName}"
				};

				_CorporateRole.RoleName = payload.RoleName;
				_CorporateRole.CorporateCustomerId = payload.CorporateCustomerId;
				_CorporateRole.ApprovalLimit = payload.ApprovalLimit;
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.CorporateRoleRepo.UpdateCorporateRole(_CorporateRole);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<CorporateRoleResponseDto>(_data: Mapper.Map<CorporateRoleResponseDto>(_CorporateRole), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("RequestRoleApproval")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<TblCorporateRole> RequestRoleApproval(string id)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateRoleApproval))
				{
					return BadRequest("UnAuthorized Access");
				}

				var roleId = Encryption.DecryptGuid(id);
				var _Role = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (_Role == null)
				{
					return BadRequest("Invalid Id.");
				}

				if (_Role.Status != 2)
				{
					return BadRequest("Role was not declined initially");
				}

				if (_Role.CorporateCustomerId == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				_Role.Status = 0;
				//UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.CorporateRoleRepo.UpdateCorporateRole(_Role);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<TblCorporateRole>(_data: _Role, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("ApproveRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<TblCorporateRole> ApproveRole(string id)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				var roleId = Encryption.DecryptGuid(id);
				var _Role = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (_Role == null)
				{
					return BadRequest("Invalid Id.");
				}

				if (_Role.Status != 0)
				{
					return BadRequest("Role was not awaiting approval initially");
				}

				if (_Role.CorporateCustomerId == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				_Role.Status = 1;
				UnitOfWork.CorporateRoleRepo.UpdateCorporateRole(_Role);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<TblCorporateRole>(_data: _Role, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("ActivateRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<TblCorporateRole> ActivateRole(string id)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ActivateCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				var roleId = Encryption.DecryptGuid(id);
				var _Role = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (_Role == null)
				{
					return BadRequest("Invalid Id.");
				}

				if (_Role.Status != -1)
				{
					return BadRequest("Role was not deactivated initially");
				}

				if (_Role.CorporateCustomerId == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				_Role.Status = 1;
				UnitOfWork.CorporateRoleRepo.UpdateCorporateRole(_Role);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<TblCorporateRole>(_data: _Role, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("DeclineRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<TblCorporateRole>> DeclineRole(string id, string reason)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeclineCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				if (string.IsNullOrEmpty(reason))
				{
					return BadRequest("Reason for declining approval is required!!!");
				}

				var roleId = Encryption.DecryptGuid(id);
				var declineReason = Encryption.DecryptStrings(reason);
				var _Role = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (_Role == null)
				{
					return BadRequest("Invalid Id.");
				}

				if (_Role.Status != 0)
				{
					return BadRequest("Role was not awaiting approval initially");
				}

				if (_Role.CorporateCustomerId == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				_Role.Status = 2;
				_Role.ReasonsForDeclining = declineReason;
				UnitOfWork.CorporateRoleRepo.UpdateCorporateRole(_Role);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<TblCorporateRole>(_data: _Role, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("DeactivateRole")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<TblCorporateRole>> DeactivateRole(string id, string reason)
		{
			try
			{
				if (!IsAuthenticated)
				{
					return StatusCode(401, "User is not authenticated");
				}

				string errormsg = string.Empty;

				if (string.IsNullOrEmpty(reason))
				{
					return BadRequest("Reason for deactivating role is required!!!");
				}

				if (!IsUserActive(out errormsg))
				{
					return StatusCode(400, errormsg);
				}

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeactivateCorporateRole))
				{
					return BadRequest("UnAuthorized Access");
				}

				var roleId = Encryption.DecryptGuid(id);
				var deactivateReason = Encryption.DecryptStrings(reason);
				var _Role = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (_Role == null)
				{
					return BadRequest("Invalid Id.");
				}

				if (_Role.Status == -1)
				{
					return BadRequest("Role has already been deactivated");
				}

				if (_Role.CorporateCustomerId == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				_Role.Status = -1;
				_Role.ReasonsForDeclining = deactivateReason;
				UnitOfWork.CorporateRoleRepo.UpdateCorporateRole(_Role);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<TblCorporateRole>(_data: _Role, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("GetRolePermissions")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<List<UserAccessModel>> GetRolePermissions(string roleId)
		{
			if (!IsAuthenticated)
			{
				return StatusCode(401, "User is not authenticated");
			}

			if (!IsUserActive(out string errormsg))
			{
				return StatusCode(400, errormsg);
			}

			if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateRolePermissions))
			{
				return BadRequest("UnAuthorized Access");
			}

			if (string.IsNullOrEmpty(roleId))
			{
				return BadRequest("Invalid id");
			}
			var corporateRoleId = Encryption.DecryptGuid(roleId);
			var _CorporateRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(corporateRoleId);

			if (_CorporateRole.CorporateCustomerId == null)
			{
				return BadRequest("UnAuthorized Access");
			}

			var permissions = UnitOfWork.CorporateUserRoleAccessRepo.GetCorporateUserPermissions(corporateRoleId.ToString()).ToList();
			return Ok(new ListResponseDTO<UserAccessModel>(_data: permissions, success: true, _message: Message.Success));
		}

		[HttpGet("GetUserAccesses")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ListResponseDTO<TblUserAccess>> GetUserAccesses()
		{
			if (!IsAuthenticated)
			{
				return StatusCode(401, "User is not authenticated");
			}

			if (!IsUserActive(out string errormsg))
			{
				return StatusCode(400, errormsg);
			}
			var UserAccesses = UnitOfWork.UserAccessRepo.GetAllCorporateUserAccesses().ToList();
			if (UserAccesses == null || UserAccesses?.Count == 0)
			{
				return StatusCode(204);
			}
			return Ok(new ListResponseDTO<TblUserAccess>(_data: UserAccesses, success: true, _message: Message.Success));
		}

		[HttpPost("AddRoleAccess")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<List<TblRoleUserAccess>> AddPermissionToCorporateRole(AddRoleAccessRequestDto model)
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

				if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.AddCorporateRolePermissions))
				{
					return BadRequest("Aunthorized Access");
				}

				//Validate Model
				if (model.AccessIds == null || model.AccessIds?.Count == 0 || string.IsNullOrEmpty(model.RoleId))
				{
					return StatusCode(400, "At least one Access Id is required");
				}
				var roleId = Encryption.DecryptGuid(model.RoleId);
				var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
				if (tblRole == null)
				{
					return BadRequest("Role id is invalid");
				}

				var tblCorporateProfile = CorporateProfile;

				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}

				if (tblRole.CorporateCustomerId == null || tblCorporateProfile.CorporateCustomerId != tblRole.CorporateCustomerId)
				{
					return BadRequest("UnAuthorized Access");
				}

				var removeAccessList = new List<TblCorporateRoleUserAccess>();
				var userAccessList = new List<TblCorporateRoleUserAccess>();
				foreach (var item in model.AccessIds)
				{
					var accessId = Encryption.DecryptGuid(item);
					var theUserAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(accessId);

					if (theUserAccess == null)
					{
						return BadRequest($"No user access with the id {accessId} exist");
					}
					var _tblRoleUserAccess = UnitOfWork.CorporateUserRoleAccessRepo.GetCorporateRoleUserAccesses(roleId.ToString(), accessId.ToString());
					if (_tblRoleUserAccess != null)
					{
						removeAccessList.Add(_tblRoleUserAccess);
						userAccessList.Add(new TblCorporateRoleUserAccess { Id = Guid.NewGuid(), CorporateRoleId = roleId.ToString(), UserAccessId = theUserAccess.Id.ToString() });
					}
					else
					{
						userAccessList.Add(new TblCorporateRoleUserAccess { Id = Guid.NewGuid(), CorporateRoleId = roleId.ToString(), UserAccessId = theUserAccess.Id.ToString() });
					}
				}
				if (removeAccessList.Count == 0)
				{
					UnitOfWork.CorporateUserRoleAccessRepo.RemoveRange(removeAccessList);
					UnitOfWork.Complete();
				}
				UnitOfWork.CorporateUserRoleAccessRepo.AddRange(userAccessList);
				UnitOfWork.Complete();
				return Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}
	}
}