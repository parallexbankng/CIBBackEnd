using System;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.BankAdmin.Controllers
{
	[ApiController]
	[Route("api/BankAdmin/v1/[controller]")]
	public class BankBranchController : BaseAPIController
	{
		private readonly IEmailService _emailService;
		private readonly ILogger<BankBranchController> _logger;
		protected readonly INotificationService notify;
		public BankBranchController(INotificationService notify, ILogger<BankBranchController> _logger, IUnitOfWork unitOfWork, AutoMapper.IMapper mapper, IHttpContextAccessor accessor, IEmailService emailService, IAuthenticationService authService) : base(mapper, unitOfWork, accessor, authService)
		{
			this._emailService = emailService;
			this._logger = _logger;
			this.notify = notify;
		}

		[HttpGet("GetAllBranches")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public async Task<ActionResult<ListResponseDTO<TblBankBranch>>> GetAllBankAdminProfiles()
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

				var bankBranches = await UnitOfWork.BranchRepo.ListAllAsync();
				if (bankBranches.Count == 0)
				{
					return StatusCode(204);
				}
				return Ok(new ListResponseDTO<TblBankBranch>(_data: bankBranches.ToList(), success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpGet("GetBrancheById")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public ActionResult<ResponseDTO<TblBankBranch>> GetBankAdminProfile(string id)
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

				var bankId = Encryption.DecryptLong(id);
				var bankBranche = UnitOfWork.BranchRepo.GetBranchById(bankId);
				if (bankBranche == null)
				{
					return BadRequest("Invalid id. Bank Branch not found");
				}
				return Ok(new ResponseDTO<TblBankBranch>(_data: bankBranche, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}
	}
}