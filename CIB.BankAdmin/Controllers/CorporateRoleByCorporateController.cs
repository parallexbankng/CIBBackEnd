
using System;
using System.Linq;
using AutoMapper;
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
    public class CorporateRoleByCorporateController : BaseAPIController
    {
        protected readonly IEmailService _emailService;
        private readonly ILogger<CorporateRoleByCorporateController> _logger;
        protected readonly INotificationService notify;
        public CorporateRoleByCorporateController(INotificationService notify,ILogger<CorporateRoleByCorporateController> _logger,IUnitOfWork unitOfWork,IEmailService emailService, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
        {
            this._emailService = emailService;
            this._logger = _logger;
            this.notify = notify;
        }

        [HttpGet("GetUserAccesses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ListResponseDTO<TblUserAccess>> GetUserAccesses()
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
                var UserAccesses = UnitOfWork.UserAccessRepo.GetAllCorporateUserAccesses().ToList();
                if (UserAccesses == null || UserAccesses?.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(new ListResponseDTO<TblUserAccess>(_data:UserAccesses,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                    //return BadRequest(ex.InnerException.Message);
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("encryption")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult<string> encryption(string item)
        {
            try
            {
                var result = Encryption.EncryptStrings(item);
                return $"{result}";
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }
        }

		[HttpPost("dencryption")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
		public ActionResult<string> dencryption(string item)
		{
			try
			{
				var result = Encryption.DecryptStrings(item);
				return $"{result}";
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
			}
		}
	}
}