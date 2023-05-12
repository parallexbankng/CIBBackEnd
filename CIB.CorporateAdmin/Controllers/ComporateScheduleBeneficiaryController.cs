
using System;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [Route("[controller]")]
    public class ComporateScheduleBeneficiaryController :  BaseAPIController
    {
        private readonly ILogger _logger;

        public ComporateScheduleBeneficiaryController(ILogger<ComporateCustomerEmployeeController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor) : base( unitOfWork, mapper,accessor)
        {
            _logger = logger;
        }

        [HttpPost("AddBeneficiary")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> AddBeneficiary(CreateBeneficiaryRequest model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }
                if(model.Beneficiaries.Count == 0)
                {
                    return BadRequest("beneficiary is empty");
                }
               
                if(string.IsNullOrEmpty(model.ScheduleId))
                {
                    return BadRequest("beneficiary is empty");
                }
                var payload = new CreateBeneficiaryRequestDto
                {
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    ScheduleId = Encryption.DecryptGuid(model.ScheduleId),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                // var validator = new CreateCorporateEmployeeValidation();
                // var results =  validator.Validate(payload);
                // if (!results.IsValid)
                // {
                //     return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                // }
                // check for duplicate

                var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId.Value);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }
                // check duplicate em
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}," +
                    $"Shedule Id: {payload.ScheduleId}, Employee Id: {payload.EmployeeId}, Amount:  {payload.Amount}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Corporate User Create Salary Schedule. Action was carried out by a Corporate user",
                    TimeStamp = DateTime.Now
                };
                var mapEmployee = Mapper.Map<TblCorporateCustomerEmployee>(payload);
                mapEmployee.Status =(int) ProfileStatus.Pending;
                //mapEmployee.CreatedBy = CorporateProfile.Id;
                mapEmployee.DateCreated = DateTime.Now;
                mapEmployee.Sn = 0;
                mapEmployee.Id = Guid.NewGuid();
                UnitOfWork.CorporateEmployeeRepo.Add(mapEmployee);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data:mapEmployee,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        // [HttpPut("RemoveBeneficiary")]
        // [ProducesResponseType(StatusCodes.Status201Created)]
        // public ActionResult<ResponseDTO<CorporateProfileResponseDto>> RequestProfileApproval(SimpleActionDto model)
        // {
        //     try
        //     {
        //         if (!IsAuthenticated)
        //         {
        //             return StatusCode(401, "User is not authenticated");
        //         }

        //         string errormsg = string.Empty;

        //         if (!IsUserActive(out errormsg))
        //         {
        //             return StatusCode(400, errormsg);
        //         }

        //         if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateUserProfileApproval))
        //         {
        //             return BadRequest("UnAuthorized Access");
        //         }

        //         var payload = new SimpleAction
        //         {
        //             Id = Encryption.DecryptGuid(model.Id),
        //             Reason = Encryption.DecryptStrings(model.Reason),
        //             IPAddress = Encryption.DecryptStrings(model.IPAddress),
        //             HostName = Encryption.DecryptStrings(model.HostName),
        //             ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
        //             MACAddress = Encryption.DecryptStrings(model.MACAddress)
        //         };
        //         var entity = UnitOfWork.TemCorporateProfileRepo.GetByIdAsync(payload.Id);
        //         if (entity == null)
        //         {
        //             return BadRequest("Invalid Id");
        //         }
                

        //         if(entity.InitiatorId != BankProfile.Id)
        //         {
        //             return BadRequest("This Request Was not Initiated By you");
        //         } 

        //         if (!RequestApproval(entity, payload, out string errorMessage))
        //         {
        //             return StatusCode(400, errorMessage);
        //         }

        //         if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
        //         {
        //             return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:new CorporateProfileResponseDto(),success:true, _message:Message.Success) );
        //         }

        //         var returnProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
        //         if (returnProfile == null)
        //         {
        //             return BadRequest("Invalid Profile Id");
        //         }
        //         return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(returnProfile),success:true, _message:Message.Success) );
        //     }
        //     catch (Exception ex)
        //     {
        //         if (ex.InnerException != null)
        //         {
        //             _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        //         }
        //         return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
        //     }
        // }

        // [HttpGet("GetBeneficiaries")]
        // [ProducesResponseType(StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status204NoContent)]
        // public ActionResult<ListResponseDTO<TblTempCorporateProfile>> GetPendingApproval(string corporateCustomerId)
        // {
        //    try
        //    {
        //      if (!IsAuthenticated)
        //     {
        //         return StatusCode(401, "User is not authenticated");
        //     }

        //     if (!IsUserActive(out string errormsg))
        //     {
        //         return StatusCode(400, errormsg);
        //     }

        //     if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateUserProfile))
        //     {
        //         return BadRequest("UnAuthorized Access");
        //     }

        //     var customerId = Encryption.DecryptGuid(corporateCustomerId);
        //     var corporateProfile = UnitOfWork.TemCorporateProfileRepo.GetCorporateProfilePendingApproval((int)ProfileStatus.Pending,customerId);
        //     if (corporateProfile == null)
        //     {
        //         return BadRequest("Invalid id. Corporate Profile not found");
        //     }

        //     //UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail {Id = Guid.NewGuid(), Username = UserName, Action = "Get Corporate Profile", Usertype = "", TimeStamp = DateTime.Now, PageName = "", Channel = "web" });
        //     //UnitOfWork.Complete();
        //     return Ok(new ListResponseDTO<TblTempCorporateProfile>(_data:corporateProfile,success:true, _message:Message.Success));
        //    }
        //    catch (Exception ex)
        //     {
        //         if (ex.InnerException != null)
        //         {
        //             _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        //         }
        //         return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
        //     }
        // }
        
        // [HttpGet("GetBeneficiariey")]
        // [ProducesResponseType(StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status204NoContent)]
        // public ActionResult<ListResponseDTO<TblTempCorporateProfile>> GetPendingApproval(string corporateCustomerId)
        // {
        //    try
        //    {
        //      if (!IsAuthenticated)
        //     {
        //         return StatusCode(401, "User is not authenticated");
        //     }

        //     if (!IsUserActive(out string errormsg))
        //     {
        //         return StatusCode(400, errormsg);
        //     }

        //     if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateUserProfile))
        //     {
        //         return BadRequest("UnAuthorized Access");
        //     }

        //     var customerId = Encryption.DecryptGuid(corporateCustomerId);
        //     var corporateProfile = UnitOfWork.TemCorporateProfileRepo.GetCorporateProfilePendingApproval((int)ProfileStatus.Pending,customerId);
        //     if (corporateProfile == null)
        //     {
        //         return BadRequest("Invalid id. Corporate Profile not found");
        //     }

        //     //UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail {Id = Guid.NewGuid(), Username = UserName, Action = "Get Corporate Profile", Usertype = "", TimeStamp = DateTime.Now, PageName = "", Channel = "web" });
        //     //UnitOfWork.Complete();
        //     return Ok(new ListResponseDTO<TblTempCorporateProfile>(_data:corporateProfile,success:true, _message:Message.Success));
        //    }
        //    catch (Exception ex)
        //     {
        //         if (ex.InnerException != null)
        //         {
        //             _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        //         }
        //         return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
        //     }
        // }
        
    }
}