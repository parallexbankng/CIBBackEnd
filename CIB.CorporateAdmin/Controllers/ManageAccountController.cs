using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Services.Api;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    public class ManageAccountController : BaseAPIController
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ManageAccountController> _logger;
        public ManageAccountController(ILogger<ManageAccountController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IApiService apiService) : base( unitOfWork, mapper,accessor)
        {
            this._apiService = apiService;
            this._logger = logger;
        }
        [HttpGet("CustomerNameInquiry")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<ResponseDTO<CustomerDataResponseDto>>> CustomerNameInquiry(string accountNumber)
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

                if (string.IsNullOrEmpty(accountNumber))
                {
                    return BadRequest("Account number is required!!!");
                }

                //call name inquiry API
                var account = Encryption.DecryptStrings(accountNumber);
                var accountInfo = await _apiService.GetCustomerDetailByAccountNumber(account);
                if(accountInfo.ResponseCode != "00")
                {
                    return BadRequest(accountInfo.ResponseDescription);
                }
                return Ok(new ResponseDTO<CustomerDataResponseDto>(_data:accountInfo,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
    }
}