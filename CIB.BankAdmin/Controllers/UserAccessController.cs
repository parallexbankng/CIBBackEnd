using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.UserAccess.Dto;
using CIB.Core.Modules.UserAccess.Validation;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.BankAdmin.Controllers
{
  [ApiController]
  [Route("api/BankAdmin/v1/[controller]")]
  public class UserAccessController : BaseAPIController
  {
    private readonly ILogger<UserAccessController> _logger;
    public UserAccessController(ILogger<UserAccessController> _logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
    {
        this._logger = _logger;
    }
    [HttpGet("GetUserAccesses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetUserAccesses()
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
          var userAccesses = await UnitOfWork.UserAccessRepo.ListAllAsync();
          if(userAccesses == null || userAccesses?.Count == 0)
          {
            return StatusCode(204);
          }
          return Ok(new ListResponseDTO<UserAccessResponseDto>(_data:Mapper.Map<List<UserAccessResponseDto>>(userAccesses),success:true, _message:Message.Success) );
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

    [HttpGet("GetUserAccess")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetUserAccess(string id)
    {
        if (!IsAuthenticated)
        {
            return StatusCode(401, "User is not authenticated");
        }
        if (!IsUserActive(out string errormsg))
        {
            return StatusCode(400, errormsg);
        }

        if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.AddRolePermissions) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.AddCorporateRolePermissions))
        {
            return BadRequest("UnAuthorized Access");
        }
        var accessId = Encryption.DecryptGuid(id);
        var UserAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(accessId);
        if (UserAccess == null)
        {
            return BadRequest("Invalid id. UserAccess not found");
        }
        return Ok(new ResponseDTO<UserAccessResponseDto>(_data:Mapper.Map<UserAccessResponseDto>(UserAccess),success:true, _message:Message.Success) );
    }

    [HttpPost("CreateUserAccess")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<UserAccessResponseDto> CreateUserAccess(GenericRequestDto model)
    {
        try
        {
            if (!IsAuthenticated)
            {
                return StatusCode(401, "User is not authenticated");
            }
            if(string.IsNullOrEmpty(model.Data))
            {
                return BadRequest("invalid request");
            }
            var requestData = JsonConvert.DeserializeObject<CreateRequestDto>(Encryption.DecryptStrings(model.Data));
            if(requestData == null)
            {
                return BadRequest("invalid request data");
            }
            var payload = new CreateRequestDto
            {
                Name = requestData.Name,
                IsCorporate = requestData.IsCorporate,
                IPAddress = Encryption.DecryptStrings(model.IPAddress),
                ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                HostName = Encryption.DecryptStrings(model.HostName)
            };
            var validator = new CreateUserAccessValidation();
            var results =  validator.Validate(payload);
            if (!results.IsValid)
            {
                return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
            }
            var mapUserAccess = Mapper.Map<TblUserAccess>(payload);
            var auditTrail = new TblAuditTrail
            {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                NewFieldValue =   $"Access Name: {payload.Name}, Is Corporate Access: {payload.IsCorporate}",
                PreviousFieldValue ="",
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = "Create Bank Admin User Access",
                TimeStamp = DateTime.Now
            };
            UnitOfWork.AuditTrialRepo.Add(auditTrail);
            UnitOfWork.UserAccessRepo.Add(mapUserAccess);
            UnitOfWork.Complete();
            return Ok(new ResponseDTO<UserAccessResponseDto>(_data:Mapper.Map<UserAccessResponseDto>(mapUserAccess),success:true, _message:Message.Success) );
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

    [HttpPost("UpdateUserAccess")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<TblUserAccess> UpdateUserAccess(GenericRequestDto model)
    {
        try
        {
            if (!IsAuthenticated)
            {
                return StatusCode(401, "User is not authenticated");
            }
            if(string.IsNullOrEmpty(model.Data))
            {
                return BadRequest("invalid request");
            }
            var requestData = JsonConvert.DeserializeObject<UpdateRequestDto>(Encryption.DecryptStrings(model.Data));
            if(requestData == null)
            {
                return BadRequest("invalid request data");
            }
            var payload = new UpdateRequestDto
            {
                Id = requestData.Id,
                Name = requestData.Name,
                IsCorporate = requestData.IsCorporate,
                IPAddress = Encryption.DecryptStrings(model.IPAddress),
                ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                HostName = Encryption.DecryptStrings(model.HostName),
                MACAddress = Encryption.DecryptStrings(model.MACAddress)
            };

            var validator = new UpdateUserAccessValidation();
            var results =  validator.Validate(payload);
            if (!results.IsValid)
            {
                return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
            }
            var userAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(payload.Id);
            if(userAccess == null)
            {
                return BadRequest("Invalid Id.");
            }
            userAccess.Name = payload.Name;
            var auditTrail = new TblAuditTrail
            {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                NewFieldValue =   $"Access Name: {payload.Name}, Is Corporate Access: {payload.IsCorporate}",
                PreviousFieldValue =$"Access Name: {userAccess.Name}, Is Corporate Access: {userAccess.IsCorporate}",
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = "Update Bank Admin User Access",
                TimeStamp = DateTime.Now
            };
            UnitOfWork.AuditTrialRepo.Add(auditTrail);
            UnitOfWork.UserAccessRepo.Update(userAccess);
            UnitOfWork.Complete();
            return Ok(new ResponseDTO<UserAccessResponseDto>(_data:Mapper.Map<UserAccessResponseDto>(userAccess),success:true, _message:Message.Success) );
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

    [HttpPost("SetCorporateUserAccess")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<bool> SetCorporateUserAccess(GenericRequestDto models)
    {
        try
        {
            if (!IsAuthenticated)
            {
                return StatusCode(401, "User is not authenticated");
            }
            if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.SetCorporateUserAccess))
            {
                return BadRequest("Aunthorized Access");
            }

            if(string.IsNullOrEmpty(models.Data))
            {
                return BadRequest("invalid request");
            }
            var requestData = JsonConvert.DeserializeObject<List<SetPermissionCreateRequestDto>>(Encryption.DecryptStrings(models.Data));
            if(requestData == null)
            {
                return BadRequest("invalid request data");
            }

            

            //Validate Model
            if (requestData.Count == 0)
            {
                return BadRequest("Model is empty");
            }


            var data = requestData.FirstOrDefault();
            var payload = new UpdateRequestDto
            {
                IPAddress = Encryption.DecryptStrings(data.IPAddress),
                ClientStaffIPAddress = Encryption.DecryptStrings(data.ClientStaffIPAddress),
                HostName = Encryption.DecryptStrings(data.HostName),
                MACAddress = Encryption.DecryptStrings(data.MACAddress)
            };
            var accessList = new List<TblUserAccess>();
            foreach(var item in requestData)
            {
                var userAccessId = item.Id;
                var IsCorporate = item.IsCorporate;
                var userAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(userAccessId);
                if (userAccess == null)
                {
                    return BadRequest($"Invalid Id {item.Id}.");
                }
                if(userAccess.IsCorporate != IsCorporate)
                {
                    userAccess.IsCorporate = IsCorporate;
                    UnitOfWork.UserAccessRepo.UpdateCorporateUserPermissions(userAccess);
                    accessList.Add(userAccess);
                }
            }
            var auditTrail = new TblAuditTrail
            {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                NewFieldValue =   $"Set Access Name: {JsonConvert.SerializeObject(accessList)}",
                PreviousFieldValue ="",
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = "Set Bank Admin User Access",
                TimeStamp = DateTime.Now
            };
            UnitOfWork.AuditTrialRepo.Add(auditTrail);
            UnitOfWork.Complete();
            return Ok(true);
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

    [HttpPost("AddRoleAccess")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<List<TblRoleUserAccess>> AddPermissionToRole(GenericRequestDto model)
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

            if(string.IsNullOrEmpty(model.Data))
            {
                return BadRequest("invalid request");
            }
            var requestData = JsonConvert.DeserializeObject<AddRoleAccessRequestDto>(Encryption.DecryptStrings(model.Data));
            if(requestData == null)
            {
                return BadRequest("invalid request data");
            }
            
            // super authorize
            if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminAuthorizer(UserRoleId))
            {
                return BadRequest("UnAuthorized Access");
            }
           
            //validate role
            var roleId = Guid.Parse(requestData.RoleId);
            var tblRole = UnitOfWork.RoleRepo.GetByIdAsync(roleId);
            if (tblRole == null)
            {
                return BadRequest("Role id is invalid");
            }

            var userAccessList = new List<TblRoleUserAccess>();
            var removeAccessList = new List<TblRoleUserAccess>();
            var previousAccessList = new List<string>();
            var newAccessList = new List<string>();
            var _previouseRoleUserAccess = UnitOfWork.UserRoleAccessRepo.GetRoleUserAccessesByRoleID(roleId.ToString());

            foreach (var item in requestData.AccessIds)
            {
                var userAccessId = Guid.Parse(item);
                var theUserAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(userAccessId);
                if (theUserAccess == null)
                {
                    return BadRequest($"No user access with the id {item} exist");
                }
                //var _tblRoleUserAccess = UnitOfWork.UserRoleAccessRepo.GetRoleUserAccessesByRoleID(roleId.ToString(),userAccessId.ToString());
                userAccessList.Add(new TblRoleUserAccess { Id = Guid.NewGuid(), RoleId = roleId.ToString(), UserAccessId = userAccessId.ToString() });
                newAccessList.Add(theUserAccess.Name);
              
            }

            foreach (var item in _previouseRoleUserAccess)
            {
                var theUserAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(Guid.Parse(item.UserAccessId));
                if (theUserAccess == null)
                {
                    return BadRequest($"No user access with the id {item} exist");
                }
                previousAccessList.Add(theUserAccess.Name);
            }
           
            if(_previouseRoleUserAccess.Count > 0)
            {
                UnitOfWork.UserRoleAccessRepo.RemoveRange(_previouseRoleUserAccess);
                UnitOfWork.Complete();
            }
            var auditTrail = new TblAuditTrail
            {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                Ipaddress = Encryption.DecryptStrings(model.IPAddress),
                Macaddress =Encryption.DecryptStrings(model.MACAddress),
                ClientStaffIpaddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                HostName = Encryption.DecryptStrings(model.HostName),
                NewFieldValue =   JsonConvert.SerializeObject(newAccessList),
                PreviousFieldValue =JsonConvert.SerializeObject(previousAccessList),
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = "Add Permission to Bank Admin User",
                TimeStamp = DateTime.Now
            };
            UnitOfWork.AuditTrialRepo.Add(auditTrail);
            UnitOfWork.UserRoleAccessRepo.AddRange(userAccessList);
            UnitOfWork.Complete();
            return Ok(true);
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

    [HttpPost("AddCorporateRoleAccess")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<List<TblRoleUserAccess>> AddPermissionToCorporateRole(GenericRequestDto model)
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

            if(string.IsNullOrEmpty(model.Data))
            {
                return BadRequest("invalid request");
            }
            var requestData = JsonConvert.DeserializeObject<AddRoleAccessRequestDto>(Encryption.DecryptStrings(model.Data));
            if(requestData == null)
            {
                return BadRequest("invalid request data");
            }

            // super authorize
            // if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminAuthorizer(UserRoleId))
            // {
            //     return BadRequest("UnAuthorized Access");
            // }
            

            if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.AddCorporateRolePermissions))
            {
                return BadRequest("Aunthorized Access");
            }
            // //validate role
            var roleId = Guid.Parse(requestData.RoleId);
            var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(roleId);
            if (tblRole == null)
            {
                return BadRequest("Role id is invalid");
            }
            var userAccessList = new List<TblCorporateRoleUserAccess>();
            var removeAccessList = new List<TblCorporateRoleUserAccess>();
            var previousAccessList = new List<string>();
            var newAccessList = new List<string>();
            
            // remove previouse one 
            var _previouseRoleUserAccess = UnitOfWork.CorporateUserRoleAccessRepo.GetCorporatePermissions(roleId);
            // create a permission with name
            foreach (var item in requestData.AccessIds)
            {
                var userAccessId = Guid.Parse(item);
                var theUserAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(userAccessId);
                if (theUserAccess == null)
                {
                    return BadRequest($"No user access with the id {item} exist");
                }
                userAccessList.Add(new TblCorporateRoleUserAccess { Id = Guid.NewGuid(), CorporateRoleId = roleId.ToString(), UserAccessId = theUserAccess.Id.ToString() });
                newAccessList.Add(theUserAccess.Name);
                // var _tblRoleUserAccess = UnitOfWork.CorporateUserRoleAccessRepo.GetCorporateRoleUserAccesses(roleId.ToString(), userAccessId.ToString());
                // if(_tblRoleUserAccess != null)
                // {
                //     previousAccessList.Add(theUserAccess.Name);
                //     newAccessList.Add(theUserAccess.Name);
                //     userAccessList.Add(new TblCorporateRoleUserAccess { Id = Guid.NewGuid(), CorporateRoleId = roleId.ToString(), UserAccessId = theUserAccess.Id.ToString() });
                // }
                // else
                // {
                //     newAccessList.Add(theUserAccess.Name);
                //     userAccessList.Add(new TblCorporateRoleUserAccess { Id = Guid.NewGuid(), CorporateRoleId = roleId.ToString(), UserAccessId = theUserAccess.Id.ToString() });
                // }
            }
            
            foreach (var item in _previouseRoleUserAccess)
            {
                //var userAccessId = Encryption.DecryptGuid(item.UserAccessId);
                var theUserAccess = UnitOfWork.UserAccessRepo.GetByIdAsync(Guid.Parse(item.UserAccessId));
                if (theUserAccess == null)
                {
                    return BadRequest($"No user access with the id {item} exist");
                }
                previousAccessList.Add(theUserAccess.Name);
            }
            
            if(_previouseRoleUserAccess.Count != 0)
            {
                UnitOfWork.CorporateUserRoleAccessRepo.RemoveRange(_previouseRoleUserAccess);
                UnitOfWork.Complete();
            }
            var auditTrail = new TblAuditTrail
            {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                Ipaddress = Encryption.DecryptStrings(model.IPAddress),
                Macaddress =Encryption.DecryptStrings(model.MACAddress),
                ClientStaffIpaddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                HostName = Encryption.DecryptStrings(model.HostName),
                NewFieldValue =   Formater.JsonType(newAccessList),
                PreviousFieldValue =Formater.JsonType(previousAccessList),
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = "Add Permission to Corporate Admin User",
                TimeStamp = DateTime.Now
            };
            UnitOfWork.AuditTrialRepo.Add(auditTrail);
            UnitOfWork.CorporateUserRoleAccessRepo.AddRange(userAccessList);
            UnitOfWork.Complete();
            return Ok(true);
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
            }
            _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
            return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
        }
    }
  }
}