using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Modules.CorporateRole.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
   [Route("api/CorporateAdmin/v1/[controller]")]
    public class CorporateRoleController : BaseAPIController
    {
        private readonly ILogger<CorporateRoleController> _logger;
        public CorporateRoleController(ILogger<CorporateRoleController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(unitOfWork,mapper,accessor,authService)
        {
            _logger = logger;
        }

        [HttpGet("GetRoles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<ListResponseDTO<CorporateRoleResponseDto>>> GetCorporateRoles()
        {
            if (!IsAuthenticated)
            {
                return StatusCode(401, "User is not authenticated");
            }

            if (!IsUserActive(out string errormsg))
            {
                return StatusCode(400, errormsg);
            }

            if (!IsUserActive(out errormsg))
            {
                return StatusCode(400, errormsg);
            }

            if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateRole))
            {
                return BadRequest("UnAuthorized Access");
            }

            var CorporateRoles = await UnitOfWork.CorporateRoleRepo.ListAllAsync();
            if (CorporateRoles.Count == 0)
            {
                return StatusCode(204);
            }
            return Ok(new ListResponseDTO<CorporateRoleResponseDto>(_data:Mapper.Map<List<CorporateRoleResponseDto>>(CorporateRoles),success:true, _message:Message.Success) );
        }

        [HttpGet("GetRole{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult GetCorporateRole(string id)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateRole))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var corporateRoleId = Encryption.DecryptGuid(id);
                var CorporateRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(corporateRoleId);
                if (CorporateRole == null)
                {
                    return BadRequest("Invalid id. CorporateRole not found");
                }

                return Ok(new ResponseDTO<CorporateRoleResponseDto>(_data:Mapper.Map<CorporateRoleResponseDto>(CorporateRole),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return Ok(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpGet("GetRolePermissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult GetRolePermissions(string roleId)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateRolePermissions))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if(string.IsNullOrEmpty(roleId))
                {
                    return BadRequest("Invalid id");
                }
                var id = Encryption.DecryptGuid(roleId);
                var permissions = UnitOfWork.CorporateUserRoleAccessRepo.GetCorporateUserPermissions(id.ToString()).ToList();
                return Ok(new ListResponseDTO<UserAccessModel>(_data:Mapper.Map<List<UserAccessModel>>(permissions),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return Ok(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }
    }
}