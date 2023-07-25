using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Modules.Role.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.BankAdmin.Controllers
{
    [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class RoleController : BaseAPIController
    {
        private readonly ILogger<RoleController> _logger;
        public RoleController(ILogger<RoleController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
        {
            this._logger = logger;
        }

        [HttpGet("GetRoles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<ListResponseDTO<RoleResponseDto>>> GetRoles()
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewRole))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var roles = await UnitOfWork.RoleRepo.ListAllAsync();
                if (roles == null || roles.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(new ListResponseDTO<RoleResponseDto>(_data:Mapper.Map<List<RoleResponseDto>>(roles),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpGet("GetRole{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ResponseDTO<RoleResponseDto>> GetRole(string id)
        {
          try
          {
              if (!IsAuthenticated)
              {
                return StatusCode(401, "User is not authenticated");
              }

              if (!IsUserActive(out string errorMsg))
              {
                return StatusCode(400, errorMsg);
              }

              if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewRole))
              {
                return BadRequest("UnAuthorized Access");
              }
              var roleId = Encryption.DecryptGuid(id);
              var role = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
              if (role == null)
              {
                return BadRequest("Invalid id. Role not found");
              }
              return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
          }
          catch (Exception ex)
          {
            _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
            return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
          }
        }

        [HttpPost("CreateRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> CreateRole(CreateRole model)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateRole))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var payload = new CreateRoleDto
                {
                    RoleName = Encryption.DecryptStrings(model.RoleName),
                    Grade = Encryption.DecryptInt(model.Grade),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                };
                var mapRole = Mapper.Map<TblRole>(payload);
                mapRole.Status = 0;
                UnitOfWork.RoleRepo.Add(mapRole);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(mapRole),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("UpdateRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> UpdateRole(UpdateRole model)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateRole))
                {
                    return BadRequest("UnAuthorized Access");
                }
                //get data
                var payload = new UpdateRoleDto
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    RoleName = Encryption.DecryptStrings(model.RoleName),
                    Grade = Encryption.DecryptInt(model.Grade)
                };
                var role = UnitOfWork.RoleRepo.GetByIdAsync(payload.Id);
                if(role == null)
                {
                    return BadRequest("Invalid Id.");
                }
                role.RoleName = payload.RoleName;
                role.Grade = payload.Grade;
                UnitOfWork.RoleRepo.UpdateRole(role);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("RequestRoleApproval")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> RequestRoleApproval(string id)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestRoleApproval))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if(string.IsNullOrEmpty(id))
                {
                    return BadRequest("Invalid Id.");
                }
                var roleId = Encryption.DecryptGuid(id);
                var role = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
                if (role == null)
                {
                    return BadRequest("Invalid Id.");
                }

                if (role.Status != 2)
                {
                    return BadRequest("Role was not declined initially");
                }

                role.Status = 0;
                UnitOfWork.RoleRepo.UpdateRole(role);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("ApproveRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> ApproveRole(string id)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveRole))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if(string.IsNullOrEmpty(id)){
                    return BadRequest("Invalid Id.");
                }
                var roleId = Encryption.DecryptGuid(id);
                var role = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
                if (role == null)
                {
                    return BadRequest("Invalid Id.");
                }

                if (role.Status != 0)
                {
                    return BadRequest("Role was not awaiting approval initially");
                }

                role.Status = 1;
                UnitOfWork.RoleRepo.UpdateRole(role);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("ActivateRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> ActivateRole(string id)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }
                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ActivateRole))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var roleId = Encryption.DecryptGuid(id);
                var role = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
                if (role == null)
                {
                    return BadRequest("Invalid Id.");
                }

                if (role.Status != -1)
                {
                    return BadRequest("Role was not deactivated initially");
                }

                role.Status = 1;
                UnitOfWork.RoleRepo.UpdateRole(role);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("DeclineRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> DeclineRole(string id, string reason)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }
                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeclineRole))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if (string.IsNullOrEmpty(reason))
                {
                    return BadRequest("Reason for declining approval role is required!!!");
                }

                var roleId = Encryption.DecryptGuid(id);
                var role = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
                if (role == null)
                {
                    return BadRequest("Invalid Id.");
                }

                if (role.Status != 0)
                {
                    return BadRequest("Role was not awaiting approval initially");
                }

                role.Status = 2;
                role.ReasonsForDeclining = Encryption.DecryptStrings(reason);
                UnitOfWork.RoleRepo.UpdateRole(role);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("DeactivateRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<RoleResponseDto>> DeactivateRole(string id, string reason)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (string.IsNullOrEmpty(reason))
                {
                    return BadRequest("Reason for deactivating role is required!!!");
                }

                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeactivateRole))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var roleId = Encryption.DecryptGuid(id);
                var role = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
                if (role == null)
                {
                    return BadRequest("Invalid Id.");
                }

                if (role.Status == -1)
                {
                    return BadRequest("Role has already been deactivated");
                }

                role.Status = -1;
                UnitOfWork.RoleRepo.UpdateRole(role);  
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<RoleResponseDto>(_data:Mapper.Map<RoleResponseDto>(role),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }
        [HttpGet("GetRolePermissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<List<RolePermissionDto>> GetRolePermissions(string roleId)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }
                if (!IsUserActive(out string errorMsg))
                {
                    return StatusCode(400, errorMsg);
                }
                var permissions =  UnitOfWork.RoleRepo.GetUserPermissions(roleId).ToList();
                return Ok(new ListResponseDTO<RolePermissionDto>(_data:permissions,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }
    }
}
