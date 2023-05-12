using System;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Services.Api;
using CIB.Core.Services.Api.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CIB.Core.Utils;
using AutoMapper;
using Microsoft.Extensions.Logging;
using CIB.Core.Common.Dto;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.BankAdmin.Controllers
{
   [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class ActiveDirectoryController : BaseAPIController
    {
        protected readonly IApiService _apiService;
        private readonly ILogger<ActiveDirectoryController> _logger;
        public ActiveDirectoryController(ILogger<ActiveDirectoryController> _logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IApiService apiService) : base(mapper, unitOfWork, accessor)
        {
            this._apiService = apiService;
            this._logger = _logger;
        }

        [HttpPost("GetBasicInfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<ResponseDTO<ADUserData>>> GetActiveDirectoryUserDetail(ADUserRequestDto model)
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

                if (string.IsNullOrEmpty(model.UserName))
                {
                    return BadRequest("Username is required");
                }

                if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payLoad = new ADUserRequestDto
                {
                    UserName = Encryption.DecryptStrings(model.UserName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress)
                };

                var result = await _apiService.ADBasicInfoInquire(payLoad.UserName);
                if (result.ResponseCode != "00")
                {
                    return BadRequest(result.ResponseDescription);
                }
                return Ok(new ResponseDTO<ADUserData>(_data:result.Data,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

    }
}