using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
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
        public BankBranchController(INotificationService notify,ILogger<BankBranchController> _logger,IUnitOfWork unitOfWork, AutoMapper.IMapper mapper, IHttpContextAccessor accessor,IEmailService emailService,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
        {
            this._emailService = emailService;
            this._logger = _logger;
            this.notify = notify;
        }

    //     [HttpGet("GetAllBranches")]
    //     [ProducesResponseType(StatusCodes.Status200OK)]
    //     [ProducesResponseType(StatusCodes.Status204NoContent)]
    //     public ActionResult<ListResponseDTO<BankAdminProfileResponse>> GetAllBankAdminProfiles()
    //     {
    //     try
    //     {
    //         if (!IsAuthenticated)
    //         {
    //             return StatusCode(401, "User is not authenticated");
    //         }

    //         if (!IsUserActive(out var errormsg))
    //         {
    //             return StatusCode(400, errormsg);
    //         }

    //         if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewBankAdminProfile))
    //         {
    //             return BadRequest("Aunthorized Access");
    //         }

    //         var corporateProfiles = UnitOfWork.BankProfileRepo.GetAllBankAdminProfiles().ToList();
    //         if (corporateProfiles.Count == 0)
    //         {
    //         return StatusCode(204);
    //         }
    //         return Ok(new ListResponseDTO<BankAdminProfileResponse>(_data:corporateProfiles,success:true, _message:Message.Success) );
    //     }
    //     catch (Exception ex)
    //     {
            
    //         _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
    //         return Ok(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
    //     }
    //     }
        
    //     [HttpPost("CreateBranche")]
    //     [ProducesResponseType(StatusCodes.Status201Created)]
    //     [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    //     public ActionResult<ResponseDTO<BankAdminProfileResponse>> CreateBankAdminProfile(CreateBankAdminProfileDTO model)
    //     {
    //     try
    //     {
    //         if (!IsAuthenticated)
    //         {
    //         return StatusCode(401, "User is not authenticated");
    //         }

    //         if (!IsUserActive(out var errormsg))
    //         {
    //         return StatusCode(400, errormsg);
    //         }

    //         if (!UnitOfWork.UserRoleAccessRepo.IsSuperAdminMaker(UserRoleId))
    //         {
    //         return BadRequest("UnAuthorized Access");
    //         }

    //         var payload = new CreateBankAdminProfileDTO
    //         {
    //         Username = Encryption.DecryptStrings(model.Username),
    //         PhoneNumber = Encryption.DecryptStrings(model.PhoneNumber),
    //         Email = Encryption.DecryptStrings(model.Email),
    //         FirstName = Encryption.DecryptStrings(model.FirstName),
    //         MiddleName = Encryption.DecryptStrings(model.MiddleName),
    //         LastName = Encryption.DecryptStrings(model.LastName),
    //         UserRoleId = Encryption.DecryptStrings(model.UserRoleId),
    //         ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
    //         HostName = Encryption.DecryptStrings(model.HostName),
    //         IPAddress = Encryption.DecryptStrings(model.IPAddress)
    //         };

    //         var validator = new CreateBankAdminProfileValidation();
    //         var results =  validator.Validate(payload);
    //         if (!results.IsValid)
    //         {
    //         return UnprocessableEntity(new ValidatorResponse(_data: new object(),_success:false,_validationResult: results.Errors));
    //         }
    //         var roleName = UnitOfWork.RoleRepo.GetRoleName(payload.UserRoleId);
    //         //var checkDuplicate
    //         payload.Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword());
    //         var mapProfile = Mapper.Map<TblBankProfile>(payload);
    //         mapProfile.Phone = payload.PhoneNumber;
    //         mapProfile.Status = (int)ProfileStatus.Pending;
    //         mapProfile.RegStage = (int)ProfileStatus.Pending;
    //         mapProfile.UserRoles = payload.UserRoleId;
        
    //         var result = UnitOfWork.BankProfileRepo.CheckDuplicate(mapProfile);
    //         if(result.IsDuplicate)
    //         {
    //         return StatusCode(400, result.Message);
    //         }

            
    //         var auditTrail = new TblAuditTrail
    //         {
    //         Id = Guid.NewGuid(),
    //         ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
    //         Ipaddress = payload.IPAddress,
    //         Macaddress = payload.MACAddress,
    //         HostName = payload.HostName,
    //         ClientStaffIpaddress = payload.ClientStaffIPAddress,
    //         NewFieldValue = $"First Name: {mapProfile.FirstName}, Last Name: {mapProfile.LastName}, Username: {mapProfile.Username}, Email Address:  {mapProfile.Email}, " +
    //         $"Middle Name: {mapProfile.MiddleName}, Phone Number: {mapProfile.Phone}, Role: {roleName},Status: {ProfileStatus.Pending}",
    //         PreviousFieldValue = "",
    //         TransactionId = "",
    //         UserId = BankProfile.Id,
    //         Username = UserName,
    //         Description = "Created a new Bank User",
    //         TimeStamp = DateTime.Now
    //         };

    //         var mapTempProfile = Mapper.Map<TblTempBankProfile>(mapProfile);
    //         var middleName = payload.MiddleName == null ? "" : payload.MiddleName.Trim().ToLower();
    //         mapProfile.FullName = payload.FirstName.Trim().ToLower() +" "+ middleName + " " +payload.LastName.Trim().ToLower();
    //         mapTempProfile.Sn = 0;
    //         mapTempProfile.Id = Guid.NewGuid();
    //         mapTempProfile.IsTreated = (int) ProfileStatus.Pending;
    //         mapTempProfile.Status = (int) ProfileStatus.Modified;
    //         mapTempProfile.InitiatorId = BankProfile.Id;
    //         mapTempProfile.InitiatorUsername = UserName;
    //         mapTempProfile.DateRequested = DateTime.Now;
    //         mapTempProfile.Action = nameof(TempTableAction.Create).Replace("_", " ");
    //         mapTempProfile.UserRoles = payload.UserRoleId;


    //         var tempResult = UnitOfWork.TemBankAdminProfileRepo.CheckDuplicate(mapProfile);
    //         if(tempResult.IsDuplicate)
    //         {
    //         return StatusCode(400, tempResult.Message);
    //         }
    //         var responseObject = Mapper.Map<BankAdminProfileResponse>(mapProfile);
    //         responseObject.PhoneNumber = mapProfile.Phone;
    //         responseObject.UserRoleName = roleName;
    //         UnitOfWork.TemBankAdminProfileRepo.Add(mapTempProfile);
    //         UnitOfWork.Complete();
    //         notify.NotifyBankAdminAuthorizer(mapTempProfile,true,"BankProfile Onboarded");
    //         return Ok(new ResponseDTO<BankAdminProfileResponse>(_data:responseObject,success:true, _message:Message.Success) );
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
    //         return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
    //     }
    //     }
        
    //     [HttpGet("GetBrancheById")]
    //     [ProducesResponseType(StatusCodes.Status200OK)]
    //     [ProducesResponseType(StatusCodes.Status204NoContent)]
    //     public ActionResult<ResponseDTO<BankAdminProfileResponse>> GetBankAdminProfile(string id)
    // {
    //   try
    //   {
    //     if (!IsAuthenticated)
    //     {
    //       return StatusCode(401, "User is not authenticated");
    //     }

    //     // string errormsg = string.Empty;

    //     if (!IsUserActive(out string errormsg))
    //     {
    //       return StatusCode(400, errormsg);
    //     }

    //     if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewBankAdminProfile))
    //     {
    //       return BadRequest("UnAuthorized Access");
    //     }

    //     var bankId = Encryption.DecryptGuid(id);
    //     var adminProfile = UnitOfWork.BankProfileRepo.GetBankAdminProfileById(bankId);
    //     if (adminProfile == null)
    //     {
    //       return BadRequest("Invalid id. Admin Profile not found");
    //     }
    //     return Ok(new ResponseDTO<BankAdminProfileResponse>(_data:adminProfile,success:true, _message:Message.Success) );
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
    //     return Ok(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
    //   }
    // }
    
    }
}