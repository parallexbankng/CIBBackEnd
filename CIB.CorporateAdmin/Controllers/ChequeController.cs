using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.Cheque.Dto;
using CIB.Core.Modules.Cheque.Validation;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;
namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class ChequeController : BaseAPIController
	{
		private readonly ILogger _logger;
		public ChequeController(ILogger<ChequeController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
		{
			_logger = logger;
		}

		[HttpPost("RequestChequeBook")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ResponseDTO<RequestChequeBookDto>> CreateCorporateProfile(RequestChequeBookDto model)
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

				// if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
				// {
				//    return BadRequest("UnAuthorized Access");
				// }
				var payload = new RequestChequeBookDto
				{
					AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
					AccountType = Encryption.DecryptStrings(model.AccountType),
					PickupBranch = Encryption.DecryptStrings(model.PickupBranch),
					NumberOfLeave = Encryption.DecryptStrings(model.NumberOfLeave),
					BranchId = Encryption.DecryptStrings(model.BranchId),
					CorporateCustomerId = Encryption.DecryptStrings(model.CorporateCustomerId),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress)
				};
				var validator = new RequestChequeValidation();
				var results = validator.Validate(payload);
				if (!results.IsValid)
				{
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}

				var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(Guid.Parse(payload.CorporateCustomerId));
				if (corporateCustomerDto == null)
				{
					return BadRequest("Invalid Corporate Customer ID");
				}

				var mapCheque = Mapper.Map<TblTempChequeRequest>(payload);
				mapCheque.InitiatorId = CorporateProfile.Id;
				mapCheque.InitiatorUsername = CorporateProfile.Username;
				mapCheque.Status = (int)ProfileStatus.Pending;
				mapCheque.DateRequested = DateTime.Now;
				mapCheque.CorporateCustomer = corporateCustomerDto.CompanyName;
				mapCheque.Sn = 0;
				mapCheque.IsTreated = 0;
				mapCheque.Action = nameof(AuditTrailAction.Create).Replace("_", " ");

				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
					Ipaddress = payload.IPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {payload.AccountNumber}, " +
						$"Account Type: {payload.AccountType}, PickupBranch: {payload.PickupBranch}, number of leave:  {payload.NumberOfLeave}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = CorporateProfile.Id,
					Username = UserName,
					Description = $"Request new Cheque Book. Action was carried out by Corporate User",
					TimeStamp = DateTime.Now
				};

				UnitOfWork.TempChequeRequestRepo.Add(mapCheque);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.Complete();
				return Ok(new ResponseDTO<TblTempChequeRequest>(_data: mapCheque, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("RequestChequeBookHistory")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public ActionResult<ListResponseDTO<ResponseChequeBookDto>> CreateCorporateProfile()
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

				// if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
				// {
				//    return BadRequest("UnAuthorized Access");
				// }
				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}
				var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
				if (corporateCustomerDto == null)
				{
					return BadRequest("Invalid Corporate Customer ID");
				}

				var checkBookHistory = UnitOfWork.ChequeRequestRepo.GetChequeRequetsByCorporateCustomer(corporateCustomerDto.Id);
				if (checkBookHistory.Any())
				{
					return Ok(new ListResponseDTO<ResponseChequeBookDto>(_data: new List<ResponseChequeBookDto>(), success: true, _message: Message.Success));
				}
				var mapResponse = Mapper.Map<List<ResponseChequeBookDto>>(checkBookHistory);
				return Ok(new ListResponseDTO<ResponseChequeBookDto>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("PendingRequestChequeBookHistory")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult<ListResponseDTO<TempResponseChequeBookDto>>> PendingRequestChequeBookHistory()
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

				// if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
				// {
				//    return BadRequest("UnAuthorized Access");
				// }
				if (CorporateProfile == null)
				{
					return BadRequest("UnAuthorized Access");
				}
				var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
				if (corporateCustomerDto == null)
				{
					return BadRequest("Invalid Corporate Customer ID");
				}
				var checkBookHistory = await UnitOfWork.ChequeRequestRepo.GetPendingChequeRequetsByCorporateCustomer(corporateCustomerDto.Id);
				if (!checkBookHistory.Any())
				{
					return Ok(new ListResponseDTO<TempResponseChequeBookDto>(_data: new List<TempResponseChequeBookDto>(), success: true, _message: Message.Success));
				}
				var mapResponse = Mapper.Map<List<TempResponseChequeBookDto>>(checkBookHistory);
				return Ok(new ListResponseDTO<TblTempChequeRequest>(_data: checkBookHistory, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}
	}
}