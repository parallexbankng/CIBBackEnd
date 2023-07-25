using System;
using System.Collections.Generic;
using System.Threading;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateProfile.Dto;
using CIB.Core.Modules.CorporateProfile.Validation;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class CorporateProfileController : BaseAPIController
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<CorporateProfileController> _logger;
        private readonly IConfiguration _config;
        protected readonly INotificationService notify;
        public CorporateProfileController(INotificationService notify,IConfiguration config,ILogger<CorporateProfileController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IEmailService emailService,IAuthenticationService authService):base(unitOfWork,mapper,accessor,authService)
        {
            this._emailService = emailService;
            this._logger = logger;
            this._config = config;
            this.notify = notify;
        }

        [HttpGet("GetAllProfiles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ListResponseDTO<CorporateProfileResponseDto>> GetAllCorporateUserProfiles(string corporateCustomerId)
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
                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }
               
                bool IsSingleSignatory = false;
                if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateUserProfile))
                {
                    if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
                    {
                        if (_authType != AuthorizationType.Single_Signatory)
                        {
                            return BadRequest("UnAuthorized Access");
                        }
                        IsSingleSignatory = true;
                    }
                    else
                    {
                        return BadRequest("Authorization type could not be determined!!!");
                    }
                }
                //GetSingleSignatoryCorporateProfilesByCorporateCustomerId
                var customerId = Encryption.DecryptGuid(corporateCustomerId);
                IEnumerable<CorporateProfileResponseDto>  corporateProfile;
                if(IsSingleSignatory)
                {
                    corporateProfile = UnitOfWork.CorporateProfileRepo.GetSingleSignatoryCorporateProfilesByCorporateCustomerId(customerId);
                }else
                {
                    corporateProfile = UnitOfWork.CorporateProfileRepo.GetAllCorporateProfilesByCorporateCustomerId(customerId);
                }
               
                if (corporateProfile == null)
                {
                    return BadRequest("Invalid id. Corporate Profile not found");
                }
                return Ok(new ListResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<List<CorporateProfileResponseDto>>(corporateProfile),success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        [HttpPost("CustomerNameEnquiry")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<CustomerNameEnquiryResponseModel> CustomerNameEnquiry(CustomerNameEnquiryModel model)
        {
            var responseModel = new CustomerNameEnquiryResponseModel();
            try
            {
                if (model == null)
                {
                    responseModel.responseCode = "400";
                    responseModel.responseDescription = "Data supplied is invalid";
                    return Ok(responseModel);
                }

                if (string.IsNullOrEmpty(model.Username))
                {
                    responseModel.responseCode = "400";
                    responseModel.responseDescription = "username is required";
                    return Ok(responseModel);
                }

                if (string.IsNullOrEmpty(model.CustomerID))
                {
                    responseModel.responseCode = "400";
                    responseModel.responseDescription = "customer id is required";
                    return Ok(responseModel);
                }

                var payload = new CustomerNameEnquiryModel
                {
                    CustomerID = Encryption.DecryptStrings(model.CustomerID),
                    Username = Encryption.DecryptStrings(model.Username)
                };

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetCorporateCustomerByCustomerID(payload.CustomerID);
                if (tblCorporateCustomer == null)
                {
                    responseModel.responseCode = "400";
                    responseModel.responseDescription = "Invalid customer id";
                    return Ok(responseModel);
                }

                var corporateProfile = UnitOfWork.CorporateProfileRepo.Find(ctx => ctx.Username == UserName && ctx.CorporateCustomerId == tblCorporateCustomer.Id);
                if (corporateProfile == null)
                {
                    responseModel.responseCode = "400";
                    responseModel.responseDescription = "No record found. Username is invalid";
                    return Ok(responseModel);
                }

                responseModel.responseCode = "00";
                responseModel.responseDescription = "Successful";
                responseModel.data = new CustomerNameEnquiryResponseDataModel
                {
                    customerEmail = corporateProfile.Email,
                    customerFirstName = corporateProfile.FirstName,
                    customerLastName = corporateProfile.LastName,
                    customerMiddleName = corporateProfile.MiddleName,
                    customerPhoneNo = corporateProfile.Phone1,
                };
                return Ok(responseModel);
            }
            catch (Exception ex)
            {
                responseModel.responseDescription = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(responseModel));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
                
            }
        }

        [HttpGet("GetProfile{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> GetCorporateUserProfile(string id)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var corporateProfileId = Encryption.DecryptGuid(id);
                var corporateProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync(corporateProfileId);
                if (corporateProfile == null)
                {
                    return BadRequest("Invalid id. Corporate Profile not found");
                }

                //UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail {Id = Guid.NewGuid(), Username = UserName, Action = "Get Corporate Profile", Usertype = "", TimeStamp = DateTime.Now, PageName = "", Channel = "web" });
                //UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(corporateProfile),success:true, _message:Message.Success));
                }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
      
        [HttpPost("CreateProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> CreateCorporateProfile(CreateProfile model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                {
                   return BadRequest("UnAuthorized Access");
                }
                var payload = new CreateProfileDto
                {
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    CorporateRoleId = Encryption.DecryptGuid(model.CorporateRoleId),
                    Username = Encryption.DecryptStrings(model.Username),
                    Phone = Encryption.DecryptStrings(model.Phone),
                    Email = Encryption.DecryptStrings(model.Email),
                    MiddleName = Encryption.DecryptStrings(model.MiddleName),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    LastName = Encryption.DecryptStrings(model.LastName),
                    FirstName = Encryption.DecryptStrings(model.FirstName),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                var validator = new CreateCorporateProfileValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId.Value);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }

                var checkAdminStatus = UnitOfWork.CorporateProfileRepo.IsAdminActive((Guid)payload.CorporateRoleId, corporateCustomerDto.Id);
                if(checkAdminStatus)
                {
                    return BadRequest($"profile with admin role already exist");
                }

                var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)payload.CorporateRoleId);
                if (tblRole == null)
                {
                    BadRequest("Invalid role id");
                }

                payload.Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword());
                var mapProfile = Mapper.Map<TblCorporateProfile>(payload);
                mapProfile.Phone1 = payload.Phone;

                var userStatus = UnitOfWork.CorporateProfileRepo.CheckDuplicate(mapProfile,corporateCustomerDto.Id);
                if(userStatus.IsDuplicate !="02")
                {
                    return StatusCode(400, userStatus.Message);
                }

                if(corporateCustomerDto.MaxAccountLimit <  payload.ApprovalLimit)
                {
                    return BadRequest($"Approval limit for this Profile must not be more that the approval limit set for the organization {corporateCustomerDto.MaxAccountLimit}");
                }

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {payload.FirstName}, " +
                    $"Last Name: {payload.LastName}, Username: {payload.Username}, Email Address:  {payload.Email}, " +
                    $"Middle Name: {payload.MiddleName}, Phone Number: {payload.Phone}, Role: {tblRole?.RoleName}, " +
                    $"Approval Limit: {payload.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Created a new Corporate User. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

               
                mapProfile.CorporateRole = payload.CorporateRoleId;
                mapProfile.Phone1 = payload.Phone;
                mapProfile.RegStage = (int) ProfileStatus.Pending;
                mapProfile.Status =(int) ProfileStatus.Pending;
                var middleName = payload.MiddleName == null ? "" : payload.MiddleName.Trim().ToLower();
                mapProfile.FullName = payload.FirstName.Trim().ToLower() +" "+ middleName + " " +payload.LastName.Trim().ToLower();
                var mapTempProfile = Mapper.Map<TblTempCorporateProfile>(mapProfile);
                mapTempProfile.IsTreated = (int) ProfileStatus.Pending;
                mapTempProfile.InitiatorId = CorporateProfile.Id;
                mapTempProfile.InitiatorUsername = UserName;
                mapTempProfile.DateRequested = DateTime.Now;
                mapProfile.Sn = 0;
                mapProfile.Id = Guid.NewGuid();
                mapTempProfile.Action = nameof(AuditTrailAction.Create).Replace("_", " ");

                var check = UnitOfWork.TemCorporateProfileRepo.CheckDuplicate(mapTempProfile,corporateCustomerDto.Id);
                if(check.IsDuplicate != "02")
                {
                    return BadRequest(check.Message);
                }
                
                var tempUserStatus = UnitOfWork.TemCorporateProfileRepo.CheckDuplicate(mapTempProfile,corporateCustomerDto.Id);
                if(tempUserStatus.IsDuplicate !="02")
                {
                    return StatusCode(400, tempUserStatus.Message);
                }


                UnitOfWork.TemCorporateProfileRepo.Add(mapTempProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(mapProfile),success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("UpdateProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> UpdateTemCorporateProfile(UpdateProfile model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var payload = new UpdateProfileDTO
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    CorporateRoleId = Encryption.DecryptGuid(model.CorporateRoleId),
                    Username = Encryption.DecryptStrings(model.Username),
                    Phone = Encryption.DecryptStrings(model.Phone),
                    Email = Encryption.DecryptStrings(model.Email),
                    FirstName = Encryption.DecryptStrings(model.FirstName),
                    MiddleName = Encryption.DecryptStrings(model.MiddleName),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    LastName = Encryption.DecryptStrings(model.LastName),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                var validator = new UpdateCorporateProfileValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                //check if corporate customer Id exist
                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId.Value);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }
                //get profile by username
                var entity = UnitOfWork.CorporateProfileRepo.GetProfileByID(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid Corporate Profile ID");
                }

                

                var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)payload.CorporateRoleId);
                if (tblRole == null)
                {
                    return BadRequest("Invalid role id");
                }

                if(entity.Status == (int)ProfileStatus.Deactivated)
                {
                    return BadRequest("Action is not allow becouse the profile is deactivated");
                }

                if(entity.Status == (int)ProfileStatus.Pending)
                {
                    return BadRequest("There is a pending request awaiting Approval");
                }


                var mapTempProfile = Mapper.Map<TblTempCorporateProfile>(entity);
                mapTempProfile.Id = Guid.NewGuid();
                mapTempProfile.Sn = 0;
                mapTempProfile.CorporateProfileId = entity.Id;
                mapTempProfile.IsTreated = 0;
                mapTempProfile.InitiatorId = CorporateProfile.Id;
                mapTempProfile.InitiatorUsername = UserName;
                mapTempProfile.DateRequested = DateTime.Now;
                mapTempProfile.FirstName = payload.FirstName;
                mapTempProfile.LastName =payload.LastName;
                mapTempProfile.MiddleName = payload.MiddleName;
                mapTempProfile.Email = payload.Email;
                mapTempProfile.Phone1 = payload.Phone;
                mapTempProfile.ApprovalLimit = payload.ApprovalLimit;
                var middleName = payload.MiddleName == null ? "" : payload.MiddleName.Trim().ToLower();
                mapTempProfile.FullName = payload.FirstName.Trim().ToLower() +" "+ middleName + " " +payload.LastName.Trim().ToLower();
                mapTempProfile.PreviousStatus = entity.Status;
                mapTempProfile.Status = (int)ProfileStatus.Modified;
                mapTempProfile.Action = nameof(AuditTrailAction.Update).Replace("_", " ");

                
                var check = UnitOfWork.TemCorporateProfileRepo.CheckDuplicate(mapTempProfile,corporateCustomerDto.Id,true);
                if(check.IsDuplicate != "02")
                {
                    return BadRequest(check.Message);
                }

                var mapProfile = Mapper.Map<TblCorporateProfile>(mapTempProfile);
                var userStatus = UnitOfWork.CorporateProfileRepo.CheckDuplicate(mapProfile,corporateCustomerDto.Id, true);
                if(userStatus.IsDuplicate !="02")
                {
                    return StatusCode(400, userStatus.Message);
                }

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =  $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {payload.FirstName}, " +
                    $"Last Name: {payload.LastName}, Username: {payload.Username}, Email Address:  {payload.Email}, " +
                    $"Middle Name: {payload.MiddleName}, Phone Number: {payload.Phone}" +
                    $"Approval Limit: {payload.ApprovalLimit}, Status: {nameof(ProfileStatus.Modified)}",
                    PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                    $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                    $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}" +
                    $"Approval Limit: {entity.ApprovalLimit}, Status: {status}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Update Corporate User. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

                var originalStatus = entity.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
                entity.Status = originalStatus;
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.TemCorporateProfileRepo.Add(mapTempProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(entity),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("RequestProfileApproval")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> RequestProfileApproval(SimpleActionDto model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateUserProfileApproval))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new SimpleAction
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Reason = Encryption.DecryptStrings(model.Reason),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                var entity = UnitOfWork.TemCorporateProfileRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid Id");
                }
                

                if(entity.InitiatorId != CorporateProfile.Id)
                {
                    return BadRequest("This Request Was not Initiated By you");
                } 

                if (!RequestApproval(entity, payload, out string errorMessage))
                {
                    return StatusCode(400, errorMessage);
                }

                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:new CorporateProfileResponseDto(),success:true, _message:Message.Success) );
                }

                var returnProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                if (returnProfile == null)
                {
                    return BadRequest("Invalid Profile Id");
                }
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(returnProfile),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("ApproveProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> ApproveProfile(SimpleActionDto model)
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
                if (string.IsNullOrEmpty(model.Id))
                {
                    return BadRequest("Invalid Id");
                }
                var payload = new SimpleAction
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var entity = UnitOfWork.TemCorporateProfileRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid Id");
                }

                if(entity.InitiatorId != CorporateProfile.Id)
                {
                    return BadRequest("This pending request was not done by you");
                }

                if(!ApprovedRequest(entity,payload, out string errorMessage))
                {
                   return StatusCode(400, errorMessage);
                }

                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:new CorporateProfileResponseDto(),success:true, _message:Message.Success) );
                }

                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(profile),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("ReActivateProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> ReActivateProfile(SimpleActionDto model)
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

                if (string.IsNullOrEmpty(model.Id))
                {
                    return BadRequest("Invalid Id");
                }

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateUserProfileApproval))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new SimpleAction
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                //var CorporateProfileId = Encryption.DecryptGuid(id);
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid Id");
                }

                if (entity.Status != -1) return BadRequest("Profile is not deactivated");

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }
                var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)entity.CorporateRole);
                if (tblRole == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =   $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {tblRole?.RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {status}, Reason: {entity.ReasonsForDeactivation}",
                    PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Profile Status: {status}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}, Reason: {entity.ReasonsForDeactivation}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Reactivated Corporate User Profile Initiated . Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                var mapTempProfile = Mapper.Map<TblTempCorporateProfile>(entity);

                mapTempProfile.Id = Guid.NewGuid();
                mapTempProfile.Sn = 0;
                mapTempProfile.CorporateProfileId = entity.Id;
                mapTempProfile.IsTreated = 0;
                mapTempProfile.InitiatorId = CorporateProfile.Id;
                mapTempProfile.InitiatorUsername = UserName;
                mapTempProfile.DateRequested = DateTime.Now;
                mapTempProfile.PreviousStatus = entity.Status;
                mapTempProfile.Status = (int)ProfileStatus.Modified;
                mapTempProfile.Action = nameof(TempTableAction.Reactivate).Replace("_", " ");
                mapTempProfile.Reasons = payload.Reason;
                UnitOfWork.TemCorporateProfileRepo.Add(mapTempProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(entity),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("DeclineProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> DeclineProfile(AppAction model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }
                if (model == null)
                {
                    return BadRequest("Invalid Request");
                }
                //var corporateProfileId = 
                //var declineRease= Encryption.DecryptStrings(model.Reason);
                var payload = new RequestCorporateProfileDto
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Reason = Encryption.DecryptStrings(model.Reason),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                };
                //get profile by id
                var entity = UnitOfWork.TemCorporateProfileRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                 return BadRequest("Invalid Id");
                }


                if(!DeclineRequest(entity,payload,out string errorMessage ))
                {
                    return StatusCode(400, errorMessage);
                }

                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:new CorporateProfileResponseDto(),success:true, _message:Message.Success) );
                }

                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(profile),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("DeactivateProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> DeactivateProfile(AppAction model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateUserProfileApproval))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if (string.IsNullOrEmpty(model.Reason))
                {
                    return BadRequest("Reason for de-activating profile is required");
                }
                //get profile by id
                var payload = new RequestCorporateProfileDto
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Reason = Encryption.DecryptStrings(model.Reason),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                };
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                return BadRequest("Invalid Id");
                }

                if (entity.Status == (int) ProfileStatus.Deactivated) return BadRequest("Profile is already de-activated");

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }

                var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)entity.CorporateRole);
                if (tblRole == null)
                {
                    return BadRequest("Invalid Corporate Role Id");
                }


                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =   $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {tblRole?.RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {status}, Reason: {entity.ReasonsForDeactivation}",
                    PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Profile Status: {status}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}, Reason: {entity.ReasonsForDeactivation}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Deactivated Corporate Profile Initiated. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                var mapTempProfile = Mapper.Map<TblTempCorporateProfile>(entity);

                mapTempProfile.Id = Guid.NewGuid();
                mapTempProfile.Sn = 0;
                mapTempProfile.CorporateProfileId = entity.Id;
                mapTempProfile.IsTreated = (int)ProfileStatus.Active;
                mapTempProfile.InitiatorId = CorporateProfile.Id;
                mapTempProfile.InitiatorUsername = UserName;
                mapTempProfile.DateRequested = DateTime.Now;
                mapTempProfile.PreviousStatus = entity.Status;
                mapTempProfile.Status = (int)ProfileStatus.Pending;
                mapTempProfile.Action = nameof(TempTableAction.Deactivate).Replace("_", " ");
                mapTempProfile.Reasons = payload.Reason;
                entity.Status = (int)ProfileStatus.Deactivated;
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.TemCorporateProfileRepo.Add(mapTempProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(entity),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("UpdateProfileUserRole")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<CorporateProfileResponseDto>> UpdateProfileUserRole(UpdateProfileRoleDto model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateUserRole))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var payload = new UpdateProfileUserRoleDTO
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    RoleId = Encryption.DecryptStrings(model.RoleId),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                //get profile by id
                var validator = new UpdateCorporateProfileUserRoleValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new object(),_success:false,_validationResult: results.Errors));
                }
                //get profile by id
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid Id");
                }

                // if(entity.Status == (int)ProfileStatus.Modified || entity.Status == (int)ProfileStatus.Pending)
                // {
                //     return BadRequest("There is a pending request awaiting Approval");
                // }

                if(entity.Status == (int)ProfileStatus.Pending)
                {
                   
                    return BadRequest("There is a pending request awaiting Approval");
                }

                var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(payload.RoleId));
                if (tblRole == null)
                {
                    BadRequest("Invalid role id");
                }
                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }

                var checkAdminStatus = UnitOfWork.CorporateProfileRepo.IsAdminActive(Guid.Parse(payload.RoleId), corporateCustomerDto.Id);
                if(checkAdminStatus)
                {
                    return BadRequest($"profile with admin role already exist");
                }


                var previousRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)entity.CorporateRole);
                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                   Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =   $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                    $"Last Name: {entity.LastName}, Role: {tblRole?.RoleName}, Status: {nameof(ProfileStatus.Modified)}",
                    PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                    $"Last Name: {entity.LastName} Role: {previousRole?.RoleName}, Status: {status}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description =$"Updated Corporate Profile Role. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

                //update existing info
                var mapTempProfile = Mapper.Map<TblTempCorporateProfile>(entity);
                mapTempProfile.Id = Guid.NewGuid();
                mapTempProfile.Sn = 0;
                mapTempProfile.CorporateProfileId = entity.Id;
                mapTempProfile.IsTreated = 0;
                mapTempProfile.InitiatorId = CorporateProfile.Id;
                mapTempProfile.InitiatorUsername = UserName;
                mapTempProfile.DateRequested = DateTime.Now;
                mapTempProfile.PreviousStatus = entity.Status;
                mapTempProfile.Status = (int)ProfileStatus.Modified;
                mapTempProfile.CorporateRole = payload.RoleId;
                mapTempProfile.Action = nameof(TempTableAction.Update_Role).Replace("_", " ");

                var originalStatus = entity.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
               
                entity.Status = originalStatus;
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.TemCorporateProfileRepo.Add(mapTempProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);

                UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(entity),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("EnableLoggedOutProfile")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<TblCorporateProfile> EnableLoggedOutProfile(CorporateProfileDto model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.EnableLoggedOutCorporateUser))
                {
                    return BadRequest("UnAuthorized Access");
                }
               
                var CorporateProfileId = Encryption.DecryptGuid(model.Id);
                var payload = new CorporateProfileDto
                {
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                };
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync(CorporateProfileId);
                if (entity == null)
                {
                    return BadRequest("Invalid Id");
                }

                if (entity.Status == 1) return BadRequest("Profile is already active");

                if (!(entity.NoOfWrongAttempts >= 3) && entity.ReasonsForDeactivation != "Multiple incorrect login attempt")
                {
                    return BadRequest("This profile can not be enabled by you");
                }

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress =payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Profile Status: {nameof(ProfileStatus.Active)}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
                    PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Profile Status: {status}, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Enabled logged out Corporate User Profile. Action was carried out by a Bank user"
                };

                //update status
                entity.Status = (int)ProfileStatus.Active;
                entity.NoOfWrongAttempts = 0;
                entity.ReasonsForDeactivation = string.Empty;
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data:Mapper.Map<CorporateProfileResponseDto>(entity),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        /// <summary>
        /// Reset User Password
        /// </summary>
        /// <param name="model">Profile id</param>
        /// <returns>Returns a boolean value indicating where the password change was successful  </returns>
        /// <response code="200">Returns a boolean value indicating where the password change was successful</response>
        /// <response code="400">If the item is null </response>
        [HttpPost("ResetUserPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> ResetUserPassword(CorporateResetPassword model)
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

            if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateUserProfile))
            {
                return BadRequest("UnAuthorized Access");
            }

            if (string.IsNullOrEmpty(model.Id))
            {
                return BadRequest("Id is required");
            }

            var payload = new CorporateResetPasswordDto
            {
                Id = Encryption.DecryptGuid(model.Id),
                // NewPassword = Encryption.DecryptStrings(model.NewPassword),
                ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                IPAddress = Encryption.DecryptStrings(model.IPAddress),
                MACAddress = Encryption.DecryptStrings(model.MACAddress),
                HostName = Encryption.DecryptStrings(model.HostName)
            };
            var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync(payload.Id);
            if (entity == null)
            {
                return BadRequest("Invalid profile id");
            }

            //check if corporate customer Id exist
            var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            if (corporateCustomerDto == null)
            {
                return BadRequest("Invalid Corporate Customer ID");
            }
            var status = (ProfileStatus)entity.Status;
            var auditTrail = new TblAuditTrail
            {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Password_Reset).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                NewFieldValue =  $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Password Reset Initiated: True, Username: {entity.Username}, First Name: {entity.FirstName}, Last Name: {entity.LastName}",
                PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " + $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}" + $"Approval Limit: {entity.ApprovalLimit}, Status: {status}",
                TransactionId = "",
                UserId = CorporateProfile.Id,
                Username = UserName,
                Description = $"Triggered password reset for Corporate User's Profile. Action was carried out by a Bank user"
            };

            bool resetInitiated = entity.ResetInitiated == 1;
            string password = PasswordValidator.GeneratePassword();
            entity.Password = Encryption.EncriptPassword(password);
            entity.ResetInitiated = entity.Passwordchanged == 1 ? 1 : 0;
            entity.DateCompleted = DateTime.Now;
            entity.PasswordExpiryDate = DateTime.Now.AddDays(30);

            UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
            UnitOfWork.AuditTrialRepo.Add(auditTrail);
            UnitOfWork.Complete();
            string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
            ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.ResetPasswordCredentialMail(entity.Email, fullName,password)));
            // return response
            return Ok(true);
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException.InnerException;
                // Log.Error(ex);
                return StatusCode(500, ex.Message);
                //return StatusCode(500,new { Responsecode = "00", ResponseDescription = "Bulk Transaction Initiated Successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
    
        [HttpGet("PendingApproval")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ListResponseDTO<TblTempCorporateProfile>> GetPendingApproval()
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

            if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateUserProfile))
            {
                return BadRequest("UnAuthorized Access");
            }
           
            var corporateProfile = UnitOfWork.TemCorporateProfileRepo.GetCorporateProfilePendingApproval((int)ProfileStatus.Pending,(Guid)CorporateProfile.CorporateCustomerId);
            if (corporateProfile == null)
            {
                return BadRequest("Invalid id. Corporate Profile not found");
            }

            //UnitOfWork.AuditTrialRepo.Add(new TblAuditTrail {Id = Guid.NewGuid(), Username = UserName, Action = "Get Corporate Profile", Usertype = "", TimeStamp = DateTime.Now, PageName = "", Channel = "web" });
            //UnitOfWork.Complete();
            return Ok(new ListResponseDTO<TblTempCorporateProfile>(_data:corporateProfile,success:true, _message:Message.Success));
           }
           catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        
        private bool ApprovedRequest(TblTempCorporateProfile requestInfo, SimpleAction payload, out string errorMessage)
        {
            
            var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)requestInfo.CorporateCustomerId);
            if (corporateCustomerDto == null)
            {
                errorMessage ="Invalid Corporate Customer ID";
                return false;
            }

            var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(requestInfo.CorporateRole));
            if (tblRole == null)
            {
                errorMessage = "Invalid role id";
                return false;
            }

            if(requestInfo.Action ==  nameof(TempTableAction.Create).Replace("_", " "))
            {

                var Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword());
                var mapProfile = Mapper.Map<TblCorporateProfile>(requestInfo);
                mapProfile.Password = Password;
                var userStatus = UnitOfWork.CorporateProfileRepo.CheckDuplicate(mapProfile,corporateCustomerDto.Id);
                if(userStatus.IsDuplicate !="02")
                {
                    errorMessage = userStatus.Message;
                    return false;
                }

                if (mapProfile.Status == (int) ProfileStatus.Active)
                {
                    errorMessage = "Profile is already active";
                    return false;
                } 

                //update status
                if (mapProfile.RegStage == 0)
                {
                    var password = Encryption.DecriptPassword(mapProfile.Password);
                    var authUrl = _config.GetValue<string>("authUrl:coporate");
                    ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.LoginCredentialMail(mapProfile.Email, mapProfile.FullName, mapProfile.Username, password, corporateCustomerDto.CustomerId,authUrl)));
                }
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
                    $"Last Name: {requestInfo.LastName}, Username: {requestInfo.Username}, Email Address:  {requestInfo.Email}, " +
                    $"Middle Name: {requestInfo.MiddleName}, Phone Number: {requestInfo.Phone1}, Role: {tblRole?.RoleName}, " +
                    $"Approval Limit: {requestInfo.ApprovalLimit}, Status: {nameof(ProfileStatus.Active)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Approved Newly Created Corporate Profile. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                requestInfo.IsTreated = (int) ProfileStatus.Active;
                requestInfo.ApprovedId = CorporateProfile.Id;
                requestInfo.ApprovalUsername = UserName;
                requestInfo.ActionResponseDate = DateTime.Now;
                mapProfile.Sn = 0;
                mapProfile.RegStage = 0;
                mapProfile.Status = (int) ProfileStatus.Active;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(requestInfo);
                UnitOfWork.CorporateProfileRepo.Add(mapProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                errorMessage = "";
                return true;
            }
            
            if(requestInfo.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
            {
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)requestInfo.CorporateProfileId);
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                     NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
                        $"Last Name: {requestInfo.LastName}, Username: {requestInfo.Username}, Email Address:  {requestInfo.Email}, " +
                        $"Middle Name: {requestInfo.MiddleName}, Phone Number: {requestInfo.Phone1}" +
                        $"Approval Limit: {requestInfo.ApprovalLimit}, Status: {nameof(ProfileStatus.Active)}",
                    PreviousFieldValue =  $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Active)}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Approved Corporate Profile Update. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

                entity.LastName = requestInfo.LastName;
                entity.FirstName = requestInfo.FirstName;
                entity.MiddleName = requestInfo.MiddleName;
                entity.Email = requestInfo.Email;
                entity.Phone1 = requestInfo.Phone1;
                entity.ApprovalLimit = requestInfo.ApprovalLimit;
                entity.FullName = requestInfo.FullName;

                var userStatus = UnitOfWork.CorporateProfileRepo.CheckDuplicate(entity,corporateCustomerDto.Id, true);
                if(userStatus.IsDuplicate !="02")
                {
                    errorMessage = userStatus.Message;
                    return false;
                }
                requestInfo.IsTreated = (int) ProfileStatus.Active;
                entity.Status = (int) ProfileStatus.Active;
                requestInfo.ApprovedId = CorporateProfile.Id;
                requestInfo.ApprovalUsername = UserName;
                requestInfo.ActionResponseDate = DateTime.Now;
                requestInfo.Reasons = payload.Reason;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(requestInfo);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                errorMessage = userStatus.Message;
                return true;
            }

            if(requestInfo.Action ==  nameof(TempTableAction.Update_Role).Replace("_", " "))
            {    
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)requestInfo.CorporateProfileId);
                var previousRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)entity.CorporateRole);
                if (previousRole == null)
                {
                    errorMessage = "Invalid role id";
                    return true;
                }
               
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {tblRole?.RoleName}," +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Modified)}",
                    PreviousFieldValue =  $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
                        $"Last Name: {requestInfo.LastName}, Username: {requestInfo.Username}, Email Address:  {requestInfo.Email}, " +
                        $"Middle Name: {requestInfo.MiddleName}, Phone Number: {requestInfo.Phone1}, Role: {previousRole?.RoleName}," +
                        $"Approval Limit: {requestInfo.ApprovalLimit}, Status: {nameof(ProfileStatus.Active)}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Approved Corporate Role Update. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

                entity.CorporateRole =Guid.Parse(requestInfo.CorporateRole);
                entity.Status = (int) ProfileStatus.Active;
                requestInfo.IsTreated = (int) ProfileStatus.Active;
                requestInfo.ApprovedId = CorporateProfile.Id;
                requestInfo.ApprovalUsername = UserName;
                requestInfo.ActionResponseDate = DateTime.Now;
                requestInfo.Reasons = payload.Reason;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(requestInfo);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                errorMessage = "";
                return false;
            }
            
            if(requestInfo.Action ==  nameof(TempTableAction.Deactivate).Replace("_", " "))
            {    
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)requestInfo.CorporateProfileId);
                var previousRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)entity.CorporateRole);
                if (previousRole == null)
                {
                    errorMessage = "Invalid role id";
                    return true;
                }
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {tblRole?.RoleName}," +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Modified)}",
                    PreviousFieldValue =  $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
                        $"Last Name: {requestInfo.LastName}, Username: {requestInfo.Username}, Email Address:  {requestInfo.Email}, " +
                        $"Middle Name: {requestInfo.MiddleName}, Phone Number: {requestInfo.Phone1}, Role: {previousRole?.RoleName}," +
                        $"Approval Limit: {requestInfo.ApprovalLimit}, Status: {nameof(ProfileStatus.Active)}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Approved Corporate Role Update. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

                entity.CorporateRole =Guid.Parse(requestInfo.CorporateRole);
                entity.Status = (int) ProfileStatus.Deactivated;
                entity.ReasonsForDeactivation = requestInfo.Reasons;
                requestInfo.Status = (int) ProfileStatus.Deactivated;
                requestInfo.IsTreated = (int) ProfileStatus.Active;
                requestInfo.ApprovedId = CorporateProfile.Id;
                requestInfo.ApprovalUsername = UserName;
                requestInfo.ActionResponseDate = DateTime.Now;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(requestInfo);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
               UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                errorMessage = "";
                return true;
            }
             
            if(requestInfo.Action ==  nameof(TempTableAction.Reactivate).Replace("_", " "))
            {    
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)requestInfo.CorporateProfileId);
                var previousRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)entity.CorporateRole);
                if (previousRole == null)
                {
                    errorMessage = "Invalid role id";
                    return true;
                }
               
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {tblRole?.RoleName}," +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Modified)}",
                    PreviousFieldValue =  $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
                        $"Last Name: {requestInfo.LastName}, Username: {requestInfo.Username}, Email Address:  {requestInfo.Email}, " +
                        $"Middle Name: {requestInfo.MiddleName}, Phone Number: {requestInfo.Phone1}, Role: {previousRole?.RoleName}," +
                        $"Approval Limit: {requestInfo.ApprovalLimit}, Status: {nameof(ProfileStatus.Active)}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Approved Corporate Profile Reactiviation. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };

                entity.Status = (int) ProfileStatus.Active;
                entity.ReasonsForDeactivation = "";
                requestInfo.Status = (int)ProfileStatus.Active;
                requestInfo.IsTreated = (int)ProfileStatus.Active;
                requestInfo.ApprovedId = CorporateProfile.Id;
                requestInfo.ApprovalUsername = UserName;
                requestInfo.ActionResponseDate = DateTime.Now;
                entity.NoOfWrongAttempts = 0;
                requestInfo.Reasons = "";
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(requestInfo);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                errorMessage = "";
                return true;
            }
            
            errorMessage = "Unknow Request";
            return false;
        }
        private bool RequestApproval(TblTempCorporateProfile entity, SimpleAction payload, out string errorMessage)
        {
            var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            if (corporateCustomerDto == null)
            {
                errorMessage ="Invalid Corporate Customer ID";
                return false;
            }

            string RoleName = " ";
            if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
            {
                if (_auth != AuthorizationType.Single_Signatory)
                {
                    var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                    if (tblRole == null)
                    {
                        errorMessage ="Invalid Role ID";
                        return false;
                    }
                    RoleName = tblRole.RoleName;
                }

            }

            if(entity.Action ==  nameof(TempTableAction.Create).Replace("_", " "))
            {
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Request Approval for new Corporate Profile. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Pending;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
                errorMessage = "Request Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }
        
                if(profile.Status == (int)ProfileStatus.Pending)
                {
                    errorMessage ="There is a pending request awaiting Approval";
                    return false;
                } 

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}" +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Request Approval for Corporate Profile Update. Action was carried out by a Corporate user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                
                entity.Status = (int) ProfileStatus.Pending;
                profile.Status = (int) ProfileStatus.Pending;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
               notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
                errorMessage = "Request Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Update_Role).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }

                if(profile.Status == (int)ProfileStatus.Pending)
                {
                    errorMessage ="There is a pending request awaiting Approval";
                    return false;
                } 

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Request Approval for Corporate Profile Role Change. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Pending;
                profile.Status = (int) ProfileStatus.Pending;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
                errorMessage = "Request Approval Was Successful";
                return true;
            }

            if(entity.Action ==  nameof(TempTableAction.Reactivate).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }

                if(profile.Status == (int)ProfileStatus.Pending)
                {
                    errorMessage ="There is a pending request awaiting Approval";
                    return false;
                } 

                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}" +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Request Approval for Corporate Profile Reactivation. Action was carried out by a Corporate user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                
                entity.Status = (int) ProfileStatus.Pending;
                //profile.Status = (int) ProfileStatus.Pending;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                //UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
                errorMessage = "Request Approval Was Successful";
                return true;
            }

            errorMessage = "invalid Request";
            return false;
        }   
        private bool DeclineRequest(TblTempCorporateProfile entity, RequestCorporateProfileDto payload, out string errorMessage)
        {
            var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);
            var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            if (corporateCustomerDto == null)
            {
                errorMessage ="Invalid Corporate Customer ID";
                return false;
            }
             string RoleName = " ";
            if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
            {
                if (_auth != AuthorizationType.Single_Signatory)
                {
                    var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                    if (tblRole == null)
                    {
                        errorMessage ="Invalid Role ID";
                        return false;
                    }
                    RoleName = tblRole.RoleName;
                }

            }

            // var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
            // if (tblRole == null)
            // {
            //     errorMessage = "Invalid role id";
            //     return false;
            // }

            var notifyInfo = new EmailNotification
            {
                CustomerId = corporateCustomerDto.CustomerId,
                FullName = entity.FullName,
                Email = entity.Email,
                PhoneNumber = entity.Phone1,
                Role = RoleName
            };

            if(entity.Action ==  nameof(TempTableAction.Create).Replace("_", " "))
            {
              
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }
                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Decline Approval for new Corporate Profile. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Declined;
                entity.IsTreated =(int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = CorporateProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                errorMessage = "Decline Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }
                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Decline Approval for Corporate Profile Update. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Declined;
                profile.Status = (int) entity.PreviousStatus;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = CorporateProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                errorMessage = "Decline Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Update_Role).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }
                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {nameof(ProfileStatus.Pending)}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Request Approval for Corporate Profile Role Change. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Declined;
                profile.Status = (int) entity.PreviousStatus;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = CorporateProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                errorMessage = "Request Approval Was Successful";
                return true;
            }

            if(entity.Action ==  nameof(TempTableAction.Deactivate).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }

                if(profile.Status == (int)ProfileStatus.Pending)
                {
                    errorMessage = "";
                    return false;
                }
                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {status}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Decline Approval for Corporate Profile Deactivation. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Declined;
                profile.Status = (int) entity.PreviousStatus;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = CorporateProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;

                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                errorMessage = "Decline Approval Was Successful";
                return true;
            }
        
            if(entity.Action ==  nameof(TempTableAction.Reactivate).Replace("_", " "))
            {
                //var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.CorporateProfileId);
                // if (tblRole == null)
                // {
                //     errorMessage = "Invalid role id";
                //     return false;
                // }
                
                if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
                {
                    errorMessage = "Profile wasn't Decline or modified initially";
                    return false;
                }
                var status = (ProfileStatus)entity.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                        $"Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Role: {RoleName}, " +
                        $"Approval Limit: {entity.ApprovalLimit}, Status: {status}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Decline Approval for Corporate Profile Reactivation. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                entity.Status = (int) ProfileStatus.Declined;
                profile.Status = (int) entity.PreviousStatus;
                profile.ReasonsForDeactivation = payload.Reason;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = CorporateProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TemCorporateProfileRepo.UpdateTempCorporateProfile(entity);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                errorMessage = "Request Approval Was Successful";
                return true;
            }
           
            errorMessage = "invalid Request";
            return false;
        }
    
    
    }
}
