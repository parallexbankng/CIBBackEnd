using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.Workflow.Dto;
using CIB.Core.Modules.Workflow.Validation;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Notification;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class WorkFlowController : BaseAPIController
    {
        private readonly ILogger<WorkFlowController> _logger;
        protected readonly INotificationService notify;
        public WorkFlowController(INotificationService notify,ILogger<WorkFlowController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(unitOfWork,mapper,accessor,authService)
        {
            _logger = logger;
            this.notify = notify;
        }

        [HttpGet("GetWorkflows")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<List<TblWorkflow>> GetWorkflows(string corporateCustomerId)
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
                var customerId = Encryption.DecryptGuid(corporateCustomerId);
                var workflows = UnitOfWork.WorkFlowRepo.GetAllWorkflow(customerId).ToList();
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if (workflows?.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(workflows);
           }
           catch (Exception ex)
           {
             _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
           }
        }

        [HttpGet("GetWorkflow")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<TblWorkflow> GetWorkflow(string corporateCustomerId)
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
                var workflowId = Encryption.DecryptGuid(corporateCustomerId);
                var workflow = UnitOfWork.WorkFlowRepo.GetWorkflowByID(workflowId);
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }
                if (workflow == null)
                {
                    return BadRequest("Invalid id. Work flow not found");
                }
                return Ok(workflow);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("CreateWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> CreateWorkflow(CreateWorkflow model)
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

                var payload = new CreateWorkflowDto
                {
                    Name = Encryption.DecryptStrings(model.Name),
                    Description = Encryption.DecryptStrings(model.Name),
                    Date = Encryption.DecryptDateTime(model.Date),
                    NoOfAuthorizers = Encryption.DecryptInt(model.NoOfAuthorizers),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    TransactionType = Encryption.DecryptStrings(model.TransactionType),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var validator = new CreateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse( _data: new Object() , _success: false,_validationResult: results.Errors));
                }

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId);
                if(corporateCustomerDto == null)
                {
                   return BadRequest("Invalid Corporate Customer Id");  
                }

                if(corporateCustomerDto.MaxAccountLimit <  payload.ApprovalLimit)
                {
                    return BadRequest($"Approval limit for this workflow must not be more that the approval limit set for the organization {corporateCustomerDto.MaxAccountLimit}");
                }

                var mapWorkFlow = Mapper.Map<TblWorkflow>(payload);
                mapWorkFlow.Date = DateTime.Now;
                mapWorkFlow.Status = (int)ProfileStatus.Modified;
                mapWorkFlow.CorporateCustomerId = payload.CorporateCustomerId;

                var checkWorkflow = UnitOfWork.WorkFlowRepo.CheckDuplicate(mapWorkFlow);
                if(checkWorkflow.IsDuplicate)
                {
                   return BadRequest(checkWorkflow.Message);     
                }

                var mapTempWorkFlow = Mapper.Map<TblTempWorkflow>(mapWorkFlow);
                var checkTempWorkflow = UnitOfWork.TempWorkflowRepo.CheckDuplicate(mapTempWorkFlow);
                if(checkTempWorkflow.IsDuplicate)
                {
                   return BadRequest(checkTempWorkflow.Message);     
                }

                mapTempWorkFlow.Sn = 0;
                mapTempWorkFlow.Action = nameof(TempTableAction.Create).Replace("_", " ");
                mapTempWorkFlow.Status = (int)ProfileStatus.Modified;
                mapTempWorkFlow.Id = Guid.NewGuid();
                mapTempWorkFlow.IsTreated = (int) ProfileStatus.Pending;
                mapTempWorkFlow.InitiatorId = CorporateProfile.Id;
                mapTempWorkFlow.InitiatorUsername = UserName;
                mapTempWorkFlow.DateRequested = DateTime.Now;

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress = payload.ClientStaffIPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {payload.Name}, Description: {payload.Description}, " +
                    $"Date: {payload.Date}, " + $"No Of Authorizers: {payload.NoOfAuthorizers}, CorporateCustomerId: {payload.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionType: {payload.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Corporate User Create Workflow",
                    TimeStamp = DateTime.Now
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.TempWorkflowRepo.Add(mapTempWorkFlow);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<WorkFlowResponseDto>(_data:Mapper.Map<WorkFlowResponseDto>(mapWorkFlow),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        
        [HttpPost("UpdateWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> UpdateWorkflow(UpdateWorkflow model)
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

                var payload = new UpdateWorkflowDto
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Name = Encryption.DecryptStrings(model.Name),
                    Description = Encryption.DecryptStrings(model.Description),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    NoOfAuthorizers = Encryption.DecryptInt(model.NoOfAuthorizers),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                };

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var validator = new UpdateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse( _data: new Object() , _success: false,_validationResult: results.Errors));
                }

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId);
                if(corporateCustomerDto == null)
                {
                   return BadRequest("Invalid Corporate Customer Id");  
                }

                if(corporateCustomerDto.MaxAccountLimit <  payload.ApprovalLimit)
                {
                    return BadRequest($"Approval limit for this workflow must not be more that the approval limit set for the organization {corporateCustomerDto.MaxAccountLimit}");
                }

                var entity = UnitOfWork.WorkFlowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }

                if(entity.Status == (int)ProfileStatus.Pending)
                {
                    return BadRequest("There is an on going modification on this workflow");
                }

                var checkDuplicate = UnitOfWork.TempWorkflowRepo.CheckTempWorkflowDuplicate(payload.Name, entity.CorporateCustomerId);
                if(checkDuplicate != null)
                {
                    return BadRequest("There is already a work flow with the same name"); 
                }

                var mapTempWorkFlow = Mapper.Map<TblTempWorkflow>(entity);

                
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {payload.Name}, Description: {payload.Description}, " +
                    $"Date: {payload.Date}, " + $"No Of Authorizers: {payload.NoOfAuthorizers}, CorporateCustomerId: {payload.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionTypet: {payload.TransactionType}",
                    PreviousFieldValue =$"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Bank User Update Workflow",
                    TimeStamp = DateTime.Now
                };
                
                mapTempWorkFlow.Sn = 0;
                mapTempWorkFlow.Name = payload.Name;
                mapTempWorkFlow.WorkflowId = entity.Id;
                mapTempWorkFlow.Description = payload.Description;
                mapTempWorkFlow.CorporateCustomerId = payload.CorporateCustomerId;
                mapTempWorkFlow.NoOfAuthorizers = payload.NoOfAuthorizers;
                mapTempWorkFlow.ApprovalLimit = payload.ApprovalLimit;
                mapTempWorkFlow.Action = nameof(TempTableAction.Update).Replace("_", " ");
                mapTempWorkFlow.PreviousStatus = entity.Status;
                mapTempWorkFlow.Status = (int)ProfileStatus.Modified;
                mapTempWorkFlow.Id = Guid.NewGuid();
                mapTempWorkFlow.IsTreated = (int) ProfileStatus.Pending;
                mapTempWorkFlow.InitiatorId = CorporateProfile.Id;
                mapTempWorkFlow.InitiatorUsername = UserName;
                mapTempWorkFlow.DateRequested = DateTime.Now;
                entity.Status = (int)ProfileStatus.Modified;
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(entity);
                UnitOfWork.TempWorkflowRepo.Add(mapTempWorkFlow);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<TblTempWorkflow>(_data: new TblTempWorkflow(),success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("UpdateTempWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> UpdateTempWorkflow(UpdateWorkflow model)
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

                var payload = new UpdateWorkflowDto
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    Name = Encryption.DecryptStrings(model.Name),
                    Description = Encryption.DecryptStrings(model.Description),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    NoOfAuthorizers = Encryption.DecryptInt(model.NoOfAuthorizers),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    IPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)

                };

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var validator = new UpdateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse( _data: new Object() , _success: false,_validationResult: results.Errors));
                }

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId);
                if(corporateCustomerDto == null)
                {
                   return BadRequest("Invalid Corporate Customer Id");  
                }

                if(corporateCustomerDto.MaxAccountLimit <  payload.ApprovalLimit)
                {
                    return BadRequest($"Approval limit for this workflow must not be more that the approval limit set for the organization {corporateCustomerDto.MaxAccountLimit}");
                }
                
                var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }

                if(entity.Status == (int)ProfileStatus.Pending)
                {
                    return BadRequest("There is an on going modification on this workflow");
                }

                entity.Name = payload.Name;
                entity.Description = payload.Description;
                entity.CorporateCustomerId = payload.CorporateCustomerId;
                entity.NoOfAuthorizers = payload.NoOfAuthorizers;
                entity.ApprovalLimit = payload.ApprovalLimit;

                //var mapWorkFlow
                var checkDuplicateRequest = UnitOfWork.TempWorkflowRepo.CheckTempDuplicateRequest(entity, nameof(TempTableAction.Update).Replace("_", " "));
                if(checkDuplicateRequest.Count != 0)
                {
                    return BadRequest("There is already and exist work flow with the same name");
                }

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {payload.Name}, Description: {payload.Description}, " +
                    $"Date: {payload.Date}, " + $"No Of Authorizers: {payload.NoOfAuthorizers}, CorporateCustomerId: {payload.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionTypet: {payload.TransactionType}",
                    PreviousFieldValue =$"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Bank User Update Workflow",
                    TimeStamp = DateTime.Now
                };

                entity.Status = (int)ProfileStatus.Modified;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();

                return Ok(new ResponseDTO<TblTempWorkflow>(_data:entity,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("RequestWorkflowApproval")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> RequestWorkflowApproval(SimpleActionDto model)
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
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };


                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestWorkflowApproval))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }
                if (entity.Status == 1)
                {
                    return BadRequest("Workflow approval cannot be requested as it is was not declined or modified");
                }

                if (CorporateProfile != null)
                {
                    if (CorporateProfile.CorporateCustomerId != entity.CorporateCustomerId)
                    {
                        return BadRequest("This Request Was not Initiated By you");
                    }
                }

                if (entity.InitiatorId != CorporateProfile.Id)
                {
                    return BadRequest("This Request Was not Initiated By you");
                }

                
                if (entity.Status == 1)
                {
                    return BadRequest("Workflow approval cannot be requested as it is was not declined or modified");
                }

                if(entity.InitiatorId != CorporateProfile.Id)
                {
                    return BadRequest("Workflow approval cannot be requested as it is was not declined or modified");
                }

                //check workflow hierarchy
                var tblWorkflowHierarchies = UnitOfWork.TempWorkflowHierarchyRepo.GetTempWorkflowHierarchyByWorkflowId(entity.Id);
                if(tblWorkflowHierarchies?.Count == 0)
                {
                    return BadRequest("No Workflow approval level has been added");
                }

                if(entity.NoOfAuthorizers != tblWorkflowHierarchies?.Count)
                {
                    return BadRequest("Number of authorizers specified in workflow must match the number of authorizers added");
                }

                if(!RequestApproval(entity, payload, out string errorMessage))
                {
                    return StatusCode(400, errorMessage);
                }


                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<WorkFlowResponseDto>(_data:new WorkFlowResponseDto(),success:true, _message:Message.Success) );
                }

                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                if (profile == null)
                {
                    return BadRequest("Invalid Profile Id");
                }
                // notifybankAuthorizer

                return Ok(new ResponseDTO<TblWorkflow>(_data:profile,success:true, _message:Message.Success) );
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
        public ActionResult<ListResponseDTO<TblTempWorkflow>> GetPendingApproval()
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

            if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
            {
                return BadRequest("UnAuthorized Access");
            }
           
            var corporateProfile = UnitOfWork.TempWorkflowRepo.GetCorporateTempWorkflowPendingApproval((int)ProfileStatus.Pending, (Guid)CorporateProfile.CorporateCustomerId);
            if (corporateProfile == null)
            {
                return BadRequest("Invalid id. Corporate Profile not found");
            }
            return Ok(new ListResponseDTO<TblTempWorkflow>(_data:corporateProfile,success:true, _message:Message.Success));
           }
           catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        private bool RequestApproval(TblTempWorkflow entity, SimpleAction payload, out string errorMessage)
        {
            var CorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            if (CorporateCustomer == null)
            {
                errorMessage = "Invalid Corporate Customer id";
                return false;
            }
            // var entity = UnitOfWork.TemBankAdminProfileRepo.GetBankProfilePendingApproval(profile,0);
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
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Request Workflow Approval for newly created Workflow",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                entity.Status = (int) ProfileStatus.Pending;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyBankAdminAuthorizer(nameof(TempTableAction.Create).Replace("_", " "), null,entity, CorporateCustomer);
                errorMessage = "Request Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
            {
                // var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                if (profile == null)
                {
                    errorMessage = "Invalid WorkFlow id";
                    return false;
                }
                
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
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Request Workflow Approval for updating Workflow",
                    TimeStamp = DateTime.Now
                };
                
                //update status
       
                entity.Status = (int) ProfileStatus.Pending;
                profile.Status = (int) ProfileStatus.Pending;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyBankAdminAuthorizer(nameof(TempTableAction.Update).Replace("_", " "), null,entity, CorporateCustomer);
                errorMessage = "Request Approval Was Successful";
                return true;
            }
            errorMessage = "invalid Request";
            return false;
        }
    }
}
