using System;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.Cheque.Dto;
using CIB.Core.Services.Notification;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.BankAdmin.Controllers
{
    [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class ChequeRequestController : BaseAPIController
    {
        private readonly ILogger _logger;
        protected readonly INotificationService notify;

        public ChequeRequestController(INotificationService notify,ILogger<ChequeRequestController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor) : base(mapper, unitOfWork, accessor)
        {
            _logger = logger;
            this.notify = notify;
        }
        
        [HttpGet("PendingChequeBookRequestList")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ListResponseDTO<TblTempChequeRequest>> PendingChequeBookRequestList()
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }
              
                var checkBookHistory = UnitOfWork.TempChequeRequestRepo.GetChequeRequestList((int)ProfileStatus.Pending);
                if (checkBookHistory == null || checkBookHistory?.Count == 0)
                {
                    return StatusCode(204);
                }

                return Ok(new ListResponseDTO<TblTempChequeRequest>(_data:checkBookHistory,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpGet("ApprovedChequeBookRequestList")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ListResponseDTO<RequestChequeBookHistory>> ApprovedChequeBookRequestList()
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

                // if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
                // {
                //     return BadRequest("UnAuthorized Access");
                // }
              
                var checkBookHistory = UnitOfWork.ChequeRequestRepo.GetChequeRequestList((int)ProfileStatus.Active);
                if (checkBookHistory == null || checkBookHistory?.Count == 0)
                {
                    return StatusCode(204);
                }

                return Ok(new ListResponseDTO<TblChequeRequest>(_data:checkBookHistory,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }


        [HttpGet("DeclineChequeBookRequestList")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ListResponseDTO<RequestChequeBookHistory>> DeclineChequeBookRequestList()
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }
              
                var checkBookHistory = UnitOfWork.TempChequeRequestRepo.GetChequeRequestList((int)ProfileStatus.Declined);
                if (checkBookHistory == null || checkBookHistory?.Count == 0)
                {
                    return StatusCode(204);
                }

                return Ok(new ListResponseDTO<TblTempChequeRequest>(_data:checkBookHistory,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPut("ApprovedChequeBookRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ResponseDTO<RequestChequeBookHistory>> ApprovedChequeBookRequest(SimpleActionDto model)
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

                // if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                // {
                //    return BadRequest("UnAuthorized Access");
                // }
                var payload = new SimpleAction
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Reason = Encryption.DecryptStrings(model.Reason),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                var entity = UnitOfWork.TempChequeRequestRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid Id");
                }
                if(!ApprovedRequest(entity,payload,out string errorMessage ))
                {
                    return StatusCode(400, errorMessage);
                }
                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<RequestChequeBookHistory>(_data:new RequestChequeBookHistory(),success:true, _message:Message.Success) );
                }

                var checkBookHistory = UnitOfWork.ChequeRequestRepo.GetByIdAsync((Guid)entity.ChequeRequetId);
                return Ok(new ResponseDTO<RequestChequeBookHistory>(_data:Mapper.Map<RequestChequeBookHistory>(checkBookHistory),success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPut("DeclineChequeBookRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ListResponseDTO<RequestChequeBookHistory>> DeclineChequeBookRequest(SimpleActionDto model)
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

                var payload = new SimpleAction
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Reason = Encryption.DecryptStrings(model.Reason),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
            
                var entity = UnitOfWork.TempChequeRequestRepo.GetByIdAsync(payload.Id);
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
                    return Ok(new ResponseDTO<RequestChequeBookHistory>(_data:new RequestChequeBookHistory(),success:true, _message:Message.Success) );
                }

                var checkBookHistory = UnitOfWork.ChequeRequestRepo.GetByIdAsync((Guid)entity.ChequeRequetId);
                return Ok(new ResponseDTO<RequestChequeBookHistory>(_data:Mapper.Map<RequestChequeBookHistory>(checkBookHistory),success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpGet("ViewChequeBookRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ResponseDTO<TblTempChequeRequest>> ViewChequeBookRequest(string chequeRequestId)
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

                // if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                // {
                //    return BadRequest("UnAuthorized Access");
                // }
                
                var id = Encryption.DecryptGuid(chequeRequestId);
                var checkBookHistory = UnitOfWork.TempChequeRequestRepo.GetByIdAsync(id);
                return Ok(new ResponseDTO<TblTempChequeRequest>(_data:checkBookHistory,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        private bool ApprovedRequest(TblTempChequeRequest profile, SimpleAction payload, out string errorMessage)
        {
            var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)profile.InitiatorId);
            var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
            if (corporateCustomerDto == null)
            {
                errorMessage ="Invalid Corporate Customer ID";
                return false;
            }

            var notifyInfo = new EmailNotification
            {
              CustomerId = corporateCustomerDto.CustomerId,
              FullName = initiatorProfile.FullName,
              Email = initiatorProfile.Email,
              Action = profile.Action,
              Reason = payload.Reason
            };

            if(profile.Action ==  nameof(TempTableAction.Create).Replace("_", " "))
            {
                var mapProfile = Mapper.Map<TblChequeRequest>(profile);
                mapProfile.ApprovedId = BankProfile.Id;
                mapProfile.ApprovalUsername = UserName;
                mapProfile.ActionResponseDate = DateTime.Now;
                if (mapProfile.Status == (int) ProfileStatus.Active)
                {
                    errorMessage = "Profile is already active";
                    return false;
                } 

                var status = (ProfileStatus)mapProfile.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {profile.AccountNumber}, " +
                    $"Account Type: {profile.AccountType}, PickupBranch: {profile.PickupBranch}, number of leave:  {profile.NumberOfLeave}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Approved Cheque Book Request Bank User Profile",
                    TimeStamp = DateTime.Now
                };
                mapProfile.Sn = 0;
                mapProfile.Status = (int)ProfileStatus.Active;
                profile.IsTreated = (int) ProfileStatus.Active;
                profile.ApprovedId = BankProfile.Id;
                profile.ApprovalUsername = UserName;
                profile.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempChequeRequestRepo.UpdateChequeRequest(profile);
                UnitOfWork.ChequeRequestRepo.Add(mapProfile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                // notify applicant that his request has been be apporved
                errorMessage = "";
                return true;
            }
            
            errorMessage = "Unknow Request";
            return false;
        }
        private bool DeclineRequest(TblTempChequeRequest entity, SimpleAction payload, out string errorMessage)
        {
            var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);
            var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            if (corporateCustomerDto == null)
            {
                errorMessage ="Invalid Corporate Customer ID";
                return false;
            }

             var notifyInfo = new EmailNotification
            {
              CustomerId = corporateCustomerDto.CustomerId,
              FullName = initiatorProfile.FullName,
              Email = initiatorProfile.Email,
              Action = entity.Action,
              Reason = entity.Reasons
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
                    NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {entity.AccountNumber}, " +
                    $"Account Type: {entity.AccountType}, PickupBranch: {entity.PickupBranch}, number of leave:  {entity.NumberOfLeave}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = $"Decline Approval for cheque book request. Action was carried out by a Bank user",
                    TimeStamp = DateTime.Now
                };
                entity.Status = (int) ProfileStatus.Declined;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = BankProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempChequeRequestRepo.UpdateChequeRequest(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
               // notify.NotifySuperAdminBankAuthorizerForBankProfileDecline(initiatorProfile,notifyInfo);
                errorMessage = " Cheque book request Decline Successful";
                return true;
            }
            errorMessage = " Invalid Request";
            return true;
        }
    
    }
}