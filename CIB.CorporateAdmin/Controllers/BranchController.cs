using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class BranchController : BaseAPIController
    {
        private readonly ILogger _logger;

        public BranchController(ILogger<BranchController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
        {
            _logger = logger;
        }

        [HttpGet("GetBranches")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<ListResponseDTO<TblBankBranch>>> GetBranches()
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

                if (CorporateProfile == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                List<TblBankBranch> branchList = (List<TblBankBranch>)await UnitOfWork.BranchRepo.ListAllAsync();
                return Ok(new ListResponseDTO<TblBankBranch>(_data: branchList, success: true, _message: Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }
    }
}