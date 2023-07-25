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
using Newtonsoft.Json;

namespace CIB.BankAdmin.Controllers
{
    [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class WorkFlowController : BaseAPIController
    {
        private readonly ILogger<WorkFlowController> _logger;
        protected readonly INotificationService notify;
        public WorkFlowController(INotificationService notify,ILogger<WorkFlowController> _logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
        {
            this._logger = _logger;
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
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if (workflows.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(workflows);
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

        [HttpGet("GetWorkflow")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<TblWorkflow> GetWorkflow(string id)
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
                var workflowId = Encryption.DecryptGuid(id);
                var workflow = UnitOfWork.WorkFlowRepo.GetWorkflowByID(workflowId);
                if (!string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }
                if (workflow == null)
                {
                    return BadRequest("Invalid id. Work flow not found");
                }
                if (workflow.Status == (int)ProfileStatus.Modified  || workflow.Status == (int)ProfileStatus.Pending)
                {
                   return BadRequest("There is either a pending modification or Approval");
                }
                return Ok(workflow);
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

        [HttpPost("CreateWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> CreateWorkflow(GenericRequestDto model)
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

                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }
                var requestData = JsonConvert.DeserializeObject<CreateWorkflowDto>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new CreateWorkflowDto
                {
                    Name = requestData.Name,
                    Description = requestData.Description,
                    Date = requestData.Date,
                    NoOfAuthorizers = requestData.NoOfAuthorizers,
                    CorporateCustomerId = requestData.CorporateCustomerId,
                    ApprovalLimit = requestData.ApprovalLimit,
                    TransactionType = requestData.TransactionType,
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                var validator = new CreateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse( _data: new Object() , _success: false,_validationResult: results.Errors));
                }

                var mapWorkFlow = Mapper.Map<TblWorkflow>(payload);
                mapWorkFlow.Date = DateTime.Now;
                mapWorkFlow.Status = (int)ProfileStatus.Modified;

                var checkWorkflow = UnitOfWork.WorkFlowRepo.CheckDuplicate(mapWorkFlow);
                if(checkWorkflow.IsDuplicate)
                {
                   return BadRequest(checkWorkflow.Message);     
                }

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId);
                if(corporateCustomerDto == null)
                {
                   return BadRequest("Invalid Corporate Customer Id");  
                }

                if(corporateCustomerDto.MaxAccountLimit <  payload.ApprovalLimit)
                {
                    return BadRequest($"Approval limit for this Profile must not be more that the approval limit set for the organization {corporateCustomerDto.MaxAccountLimit}");
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
                mapTempWorkFlow.InitiatorId = BankProfile.Id;
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
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Bank User Create Workflow",
                    TimeStamp = DateTime.Now
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.TempWorkflowRepo.Add(mapTempWorkFlow);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<WorkFlowResponseDto>(_data:Mapper.Map<WorkFlowResponseDto>(mapWorkFlow),success:true, _message:Message.Success) );
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

        [HttpPost("UpdateWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> UpdateWorkflow(GenericRequestDto model)
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

                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }
                var requestData = JsonConvert.DeserializeObject<CreateWorkflowDto>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new UpdateWorkflowDto
                {
                    Id = requestData.Id,
                    Name = requestData.Name,
                    Description = requestData.Description,
                    CorporateCustomerId = requestData.CorporateCustomerId,
                    NoOfAuthorizers = requestData.NoOfAuthorizers,
                    ApprovalLimit = requestData.ApprovalLimit,
                    IPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                var validator = new UpdateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse( _data: new Object() , _success: false,_validationResult: results.Errors));
                }
                var entity = UnitOfWork.WorkFlowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }

                if(entity.Status == (int)ProfileStatus.Pending || entity.Status  == (int)ProfileStatus.Modified)
                {
                    return BadRequest("There is already a pending modification or approval for this workflow");
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

                var checkDuplicateRequest = UnitOfWork.TempWorkflowRepo.CheckTempWorkflowDuplicate(payload.Name, entity.CorporateCustomerId);
                if(checkDuplicateRequest != null)
                {
                    return BadRequest("There is already a work flow with the same name"); 
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
                    $"TransactionType: {payload.TransactionType}",
                    PreviousFieldValue =$"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionType: {entity.TransactionType}",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Bank User Update Workflow",
                    TimeStamp = DateTime.Now
                };

                var updateTemp = new TblTempWorkflow
                {
                    Id = Guid.NewGuid(),
                    Sn = 0,
                    WorkflowId = entity.Id,
                    Name = payload.Name,
                    Description = payload.Description,
                    CorporateCustomerId = entity.CorporateCustomerId,
                    NoOfAuthorizers = payload.NoOfAuthorizers,
                    ApprovalLimit = payload.ApprovalLimit,
                    Status = (int)ProfileStatus.Modified,
                    PreviousStatus = entity.Status,
                    Action = nameof(TempTableAction.Update).Replace("_", " "),
                    IsTreated = (int)ProfileStatus.Pending,
                    DateRequested = DateTime.Now,
                    InitiatorId = BankProfile.Id,
                    InitiatorUsername = UserName,
                };
                
                var getWorkflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(entity.Id);
                var tblWorkflowHierarchies = new List<TblTempWorkflowHierarchy>();
                if(getWorkflowHierarchies?.Count > 0)
                {
                    foreach (var item in getWorkflowHierarchies)
                    {
                        var newWorkFlow = new TblTempWorkflowHierarchy
                        {
                            Id = Guid.NewGuid(),
                            Sn = 0,
                            WorkflowId = updateTemp.Id,
                            AuthorizationLevel = item.AuthorizationLevel,
                            ApproverId = item.ApproverId,
                            ApproverName = item.ApproverName
                        };
                        tblWorkflowHierarchies.Add(newWorkFlow);
                    }
                }

                if(tblWorkflowHierarchies.Count > 0)
                {
                    UnitOfWork.TempWorkflowHierarchyRepo.AddRange(tblWorkflowHierarchies);
                }

                

                entity.Status = (int)ProfileStatus.Modified;
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(entity);
                UnitOfWork.TempWorkflowRepo.Add(updateTemp);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<WorkFlowResponseDto>(_data:Mapper.Map<WorkFlowResponseDto>(entity),success:true, _message:Message.Success) );
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

        [HttpPost("UpdateTempWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> UpdateTempWorkflow(GenericRequestDto model)
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

                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }
                var requestData = JsonConvert.DeserializeObject<UpdateWorkflowDto>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }

                var payload = new UpdateWorkflowDto
                {
                    Id = requestData.Id,
                    Name = requestData.Name,
                    Description = requestData.Description,
                    CorporateCustomerId = requestData.CorporateCustomerId,
                    NoOfAuthorizers = requestData.NoOfAuthorizers,
                    ApprovalLimit = requestData.ApprovalLimit,
                    IPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)

                };

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var validator = new UpdateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse( _data: new Object() , _success: false,_validationResult: results.Errors));
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
                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId);
                if(corporateCustomerDto == null)
                {
                   return BadRequest("Invalid Corporate Customer Id");  
                }

                if(corporateCustomerDto.MaxAccountLimit <  payload.ApprovalLimit)
                {
                    return BadRequest($"Approval limit for this workflow must not be more that the approval limit set for the organization {corporateCustomerDto.MaxAccountLimit}");
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

                var checkDuplicate = UnitOfWork.TempWorkflowRepo.CheckTempWorkflowDuplicate(payload.Name, entity.CorporateCustomerId);
                if(checkDuplicate != null)
                {
                    return BadRequest("There is already a work flow with the same name"); 
                }
                
                //entity.Status = (int)ProfileStatus.Modified;
                // //mapTempWorkFlow.Action = nameof(TempTableAction.Create).Replace("_", " ");
                

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {payload.Name}, Description: {payload.Description}, " +
                    $"Date: {payload.Date}, " + $"No Of Authorizers: {payload.NoOfAuthorizers}, CorporateCustomerId: {payload.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionType: {payload.TransactionType}",
                    PreviousFieldValue =$"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionType: {entity.TransactionType}",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Bank User Update Workflow",
                    TimeStamp = DateTime.Now
                };

                var originalStatus = entity.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Modified;
                entity.Status = originalStatus;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();

                return Ok(new ResponseDTO<TblTempWorkflow>(_data:entity,success:true, _message:Message.Success) );
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

        [HttpPost("RequestWorkflowApproval")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkFlowResponseDto>> RequestWorkflowApproval(GenericRequestDto model)
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
                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }
                var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestWorkflowApproval))
                {
                    return BadRequest("UnAuthorized Access");
                }

                //var workflowId = Encryption.DecryptGuid(id);
                var payload = new SimpleAction
                {
                    Id = requestData.Id,
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }
                if (entity.Status == 1)
                {
                    return BadRequest("Workflow approval cannot be requested as it is was not declined or modified");
                }

                if(entity.InitiatorId != BankProfile.Id)
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

                if (entity.WorkflowId == null)
                {
                  return BadRequest("Invalid Profile Id");
                }

                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                if (profile == null)
                {
                  return BadRequest("Invalid Profile Id");
                }
                return Ok(new ResponseDTO<TblWorkflow>(_data:profile,success:true, _message:Message.Success) );
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

        [HttpPost("ApproveWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<TblWorkflow>> ApproveWorkflow(GenericRequestDto model)
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
                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }
                var requestData = JsonConvert.DeserializeObject<SimpleAction>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }

            
                var payload = new SimpleAction
                {
                    Id = requestData.Id,
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }

                if (entity.Status != (int) ProfileStatus.Pending)
                {
                    return BadRequest("Workflow can not be Approved as it is not awaiting approval");
                }

                if(!ApprovedRequest(entity,payload, out string errorMessage))
                {
                   return StatusCode(400, errorMessage);
                }

                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<WorkFlowResponseDto>(_data:new WorkFlowResponseDto(),success:true, _message:Message.Success) );
                }

                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                return Ok(new ResponseDTO<TblWorkflow>(_data:profile,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                //return BadRequest(ex.InnerException.Message);
                }
                 _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("DeclineWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<TblWorkflow>> DeclineWorkflow(GenericRequestDto model)
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
                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }
                var requestData = JsonConvert.DeserializeObject<AppActionDto>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeclineWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var payload = new AppActionDto
                {
                    Id = requestData.Id,
                    Reason = requestData.Reason,
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                if(string.IsNullOrEmpty(payload.Reason))
                {
                    return BadRequest("Reason for declining workflow is required");
                }

                var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }

                if(!DeclineRequest(entity,payload,out string errorMessage ))
                {
                    return StatusCode(400, errorMessage);
                }

                if(entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
                {
                    return Ok(new ResponseDTO<WorkFlowResponseDto>(_data:new WorkFlowResponseDto(),success:true, _message:Message.Success) );
                }
                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                return Ok(new ResponseDTO<TblWorkflow>(_data:profile,success:true, _message:Message.Success) );
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
   
        [HttpGet("PendingApproval")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ListResponseDTO<TblTempWorkflow>> GetPendingApproval(string corporateCustomerId)
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

            if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
            {
                return BadRequest("UnAuthorized Access");
            }
           
           var CustomerId = Encryption.DecryptGuid(corporateCustomerId); 
            var corporateProfile = UnitOfWork.TempWorkflowRepo.GetCorporateTempWorkflowPendingApproval((int)ProfileStatus.Pending,CustomerId);
            if (corporateProfile == null)
            {
                return BadRequest("Invalid id. Corporate Profile not found");
            }
            return Ok(new ListResponseDTO<TblTempWorkflow>(_data:corporateProfile,success:true, _message:Message.Success));
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
       
        [HttpPost("BulkRequestApproved")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
        public ActionResult<bool> BulkRequestApproved(GenericRequestDto model)
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

                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }

                var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }
               
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ApproveCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var responseErrors = new List<BulkError>();
                foreach(var item in requestData)
                {

                    var payload = new SimpleAction
                    {
                        Id = item.Id,
                        IPAddress = Encryption.DecryptStrings(model.IPAddress),
                        HostName = Encryption.DecryptStrings(model.HostName),
                        ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                        MACAddress = Encryption.DecryptStrings(model.MACAddress)
                    };
                    var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                    if (entity == null)
                    {
                        var bulkError = new BulkError
                        {
                            Message = "Invalid Id. Workflow does not exist",
                            ActionInfo = $"WorkflowId : {payload.Id}"
                        };
                        responseErrors.Add(bulkError);
                    }
                    else
                    {
                        if (entity.Status != (int) ProfileStatus.Pending)
                        {
                            var bulkError = new BulkError
                            {
                                Message = "Workflow can not be Approved as it is not awaiting approval",
                                ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                            };
                            responseErrors.Add(bulkError);
                        }
                        else
                        {
                            if(!ApprovedRequest(entity,payload, out string errorMessage))
                            {
                                var bulkError = new BulkError
                                {
                                    Message = errorMessage,
                                    ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                                };
                                responseErrors.Add(bulkError);
                            }
                        }

                    }
                }
                if(responseErrors.Any())
                {
                    return BadRequest(responseErrors);
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("BulkRequestDecline")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
        public ActionResult<bool> BulkRequestDecline(GenericRequestDto model)
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

                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }

                var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeclineWorkflow))
                {
                    return BadRequest("UnAuthorized Access");
                }
                var responseErrors = new List<BulkError>();
                foreach(var item in requestData)
                {
                    var payload = new AppActionDto
                    {
                        Id = item.Id,
                        Reason = item.Reason,
                        IPAddress = Encryption.DecryptStrings(model.IPAddress),
                        HostName = Encryption.DecryptStrings(model.HostName),
                        ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                        MACAddress = Encryption.DecryptStrings(model.MACAddress)
                    };

                    if(string.IsNullOrEmpty(payload.Reason))
                    {
                        var bulkError = new BulkError
                        {
                            Message = "Reason for declining workflow is required",
                            ActionInfo = $"WorkflowId : {payload.Id}"
                        };
                        responseErrors.Add(bulkError);
                    }

                    var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                    if (entity == null)
                    {
                        var bulkError = new BulkError
                        {
                           Message = "Invalid Id. Workflow does not exist",
                            ActionInfo = $"WorkflowId : {payload.Id}"
                        };
                        responseErrors.Add(bulkError);
                    }
                    else
                    {              
                        if(!DeclineRequest(entity,payload,out string errorMessage ))
                        {
                            var bulkError = new BulkError
                            {
                                Message = errorMessage,
                                ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                            };
                            responseErrors.Add(bulkError);
                        }         
                    }
                }
                if(responseErrors.Any())
                {
                    return BadRequest(responseErrors);
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
    
        [HttpPost("BulkRequestApproval")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
        public ActionResult<bool> BulkRequestApproval(GenericRequestDto model)
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

                if(string.IsNullOrEmpty(model.Data))
                {
                    return BadRequest("invalid request");
                }

                var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
                if(requestData == null)
                {
                    return BadRequest("invalid request data");
                }
                var responseErrors = new List<BulkError>();
                foreach(var item in requestData)
                {
                    var payload = new SimpleAction
                    {
                        Id = item.Id,
                        IPAddress = Encryption.DecryptStrings(model.IPAddress),
                        HostName = Encryption.DecryptStrings(model.HostName),
                        ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                        MACAddress = Encryption.DecryptStrings(model.MACAddress)
                    };
                    var entity = UnitOfWork.TempWorkflowRepo.GetByIdAsync(payload.Id);
                    if (entity == null)
                    {
                        var bulkError = new BulkError
                        {
                            Message = "Invalid Id. Workflow does not exist",
                            ActionInfo = $"WorkflowId : {payload.Id}"
                        };
                        responseErrors.Add(bulkError);
                    }
                    else
                    {
                        if (entity.Status == 1)
                        {
                            var bulkError = new BulkError
                            {
                                Message = "Workflow approval cannot be requested as it is was not declined or modified",
                                ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                            };
                            responseErrors.Add(bulkError);
                        }
                        else
                        {

                            if(entity.InitiatorId != BankProfile.Id)
                            {
                                var bulkError = new BulkError
                                {
                                    Message = "request was not initiated by you",
                                    ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                                };
                                responseErrors.Add(bulkError);
                            }
                            else
                            {
                                var tblWorkflowHierarchies = UnitOfWork.TempWorkflowHierarchyRepo.GetTempWorkflowHierarchyByWorkflowId(entity.Id);
                                if(tblWorkflowHierarchies?.Count == 0)
                                {
                                    var bulkError = new BulkError
                                    {
                                        Message = "No Workflow approval level has been added",
                                         ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                                    };
                                    responseErrors.Add(bulkError);
                                }
                                else
                                {
                                    if(entity.NoOfAuthorizers != tblWorkflowHierarchies?.Count)
                                    {
                                        var bulkError = new BulkError
                                        {
                                            Message = "Number of authorizers specified in workflow must match the number of authorizers added",
                                            ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                                        };
                                        responseErrors.Add(bulkError);
                                    }
                                    else
                                    {
                                        if(!RequestApproval(entity, payload, out string errorMessage))
                                        {
                                            var bulkError = new BulkError
                                            {
                                                Message = errorMessage,
                                                ActionInfo = $"Workflow : {entity.Description}, Action: {entity.Action}"
                                            };
                                            responseErrors.Add(bulkError);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if(responseErrors.Any())
                {
                    return BadRequest(responseErrors);
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
    
    
        private bool ApprovedRequest(TblTempWorkflow entity, SimpleAction payload, out string errorMessage)
        {
          if (entity.CorporateCustomerId != null)
          {
            var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            if (corporateCustomer == null)
            {
              errorMessage = "Invalid Corporate Customer";
              return false;
            }
          }
          if(entity.Action ==  nameof(TempTableAction.Create).Replace("_", " "))
          {
                var mapWorkflow = Mapper.Map<TblWorkflow>(entity);
                var userStatus = UnitOfWork.WorkFlowRepo.CheckDuplicate(mapWorkflow);
                if(userStatus.IsDuplicate)
                {
                    errorMessage = userStatus.Message;
                    return false;
                }

                if (mapWorkflow.Status == (int) ProfileStatus.Active)
                {
                    errorMessage = "Workflow is already active";
                    return false;
                } 

                var status = (ProfileStatus)mapWorkflow.Status;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Workflow Approval ",
                    TimeStamp = DateTime.Now
                };
                //update status
                
                mapWorkflow.Id = Guid.NewGuid();
                var getWorkflowHierarchies = UnitOfWork.TempWorkflowHierarchyRepo.GetTempWorkflowHierarchyByWorkflowId(entity.Id);
                var tblWorkflowHierarchies = new List<TblWorkflowHierarchy>();
                if(getWorkflowHierarchies?.Count > 0)
                {
                    foreach (var item in getWorkflowHierarchies)
                    {
                        var newWorkFlow = new TblWorkflowHierarchy
                        {
                            Id = Guid.NewGuid(),
                            Sn = 0,
                            WorkflowId = mapWorkflow.Id,
                            AuthorizationLevel = item.AuthorizationLevel,
                            ApproverId = item.ApproverId,
                            ApproverName = item.ApproverName
                        };
                        tblWorkflowHierarchies.Add(newWorkFlow);
                    }
                }

                if(tblWorkflowHierarchies?.Count > 0)
                {
                    UnitOfWork.WorkFlowHierarchyRepo.AddRange(tblWorkflowHierarchies);
                }

                mapWorkflow.Sn = 0;
                mapWorkflow.ApprovalLimit = entity.ApprovalLimit;
                mapWorkflow.Status = (int)ProfileStatus.Active;
                entity.IsTreated = (int) ProfileStatus.Active;
                entity.ApprovedId = BankProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.WorkFlowRepo.Add(mapWorkflow);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                errorMessage = "";
                return true;
          }
          if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
          {
            if (entity.WorkflowId != null)
            {
              var workflow = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
              workflow.Name = entity.Name;
              workflow.Description = entity.Description;
              workflow.ApprovalLimit = entity.ApprovalLimit;
              workflow.NoOfAuthorizers = entity.NoOfAuthorizers;

              var userStatus = UnitOfWork.WorkFlowRepo.CheckDuplicate(workflow, true);
              if (userStatus.IsDuplicate)
              {
                errorMessage = userStatus.Message;
                return false;
              }
              
              //var toBeDeleted = UnitOfWork.WorkFlowHierarchyRepo.GetByIdAsync(workflow.Id);
              string newRecords = string.Empty;
              string oldRecords = string.Empty;
              var deleteWorkflowHierarchiesList =
                UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(workflow.Id);

              if (deleteWorkflowHierarchiesList?.Count != 0 && deleteWorkflowHierarchiesList != null)
              {
                foreach (var item in deleteWorkflowHierarchiesList)
                {
                  oldRecords += $"Approver: {item.ApproverName}, Level: {item.AuthorizationLevel} \n";
                }

                UnitOfWork.WorkFlowHierarchyRepo.RemoveRange(deleteWorkflowHierarchiesList);
              }

              var workflowHierarchiesList = UnitOfWork.TempWorkflowHierarchyRepo.GetTempWorkflowHierarchyByWorkflowId(entity.Id);
              var tblWorkflowHierarchies = new List<TblWorkflowHierarchy>();
              if (workflowHierarchiesList?.Count > 0)
              {
                foreach (var item in workflowHierarchiesList)
                {
                  var newWorkFlow = new TblWorkflowHierarchy
                  {
                    Id = Guid.NewGuid(),
                    WorkflowId = workflow.Id,
                    AuthorizationLevel = item.AuthorizationLevel,
                    ApproverId = item.ApproverId,
                    ApproverName = item.ApproverName
                  };
                  tblWorkflowHierarchies.Add(newWorkFlow);
                }
              }

              if (tblWorkflowHierarchies?.Count > 0)
              {
                foreach (var item in tblWorkflowHierarchies)
                {
                  newRecords += $"Approver: {item.ApproverName}, Level: {item.AuthorizationLevel} \n";
                }

                UnitOfWork.WorkFlowHierarchyRepo.AddRange(tblWorkflowHierarchies);
              }

              var auditTrail = new TblAuditTrail
              {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                NewFieldValue = $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                                $"Date: {entity.Date}, " +
                                $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                                $"TransactionType: {entity.TransactionType}",
                PreviousFieldValue = "",
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = $"Approved Work Flow Update. Action was carried out by a Bank user",
                TimeStamp = DateTime.Now
              };


              var originalStatus = entity.Status == (int)ProfileStatus.Deactivated
                ? (int)ProfileStatus.Deactivated
                : (int)ProfileStatus.Active;
              entity.IsTreated = (int)ProfileStatus.Active;
              workflow.Status = (int)ProfileStatus.Active;
              entity.Status = originalStatus;
              entity.ApprovedId = BankProfile.Id;
              entity.ApprovalUsername = UserName;
              entity.ActionResponseDate = DateTime.Now;
              entity.Reasons = payload.Reason;

              UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
              UnitOfWork.WorkFlowRepo.UpdateWorkflow(workflow);
              UnitOfWork.AuditTrialRepo.Add(auditTrail);
              UnitOfWork.Complete();
              errorMessage = "";
              return true;
            }
            errorMessage = "Unknown Request";
            return false;
          }
          if(entity.Action ==  nameof(TempTableAction.Deactivate).Replace("_", " "))
          {
            if (entity.WorkflowId != null)
            {
              var workflow = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
              if (workflow == null)
              {
                errorMessage = "Invalid Workflow id";
                return false;
              }
                
              var auditTrail = new TblAuditTrail
              {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                                  $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                                  $"TransactionType: {entity.TransactionType}",
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = $"Approved WorkFlow Deactivation. Action was carried out by a Bank user",
                TimeStamp = DateTime.Now
              };
              
              workflow.Status = (int) ProfileStatus.Deactivated;
              workflow.ReasonForDeclining = entity.Reasons;
              entity.Status = (int) ProfileStatus.Deactivated;
              entity.IsTreated = (int) ProfileStatus.Active;
              entity.ApprovedId = BankProfile.Id;
              entity.ApprovalUsername = UserName;
              entity.ActionResponseDate = DateTime.Now;

              UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
              UnitOfWork.WorkFlowRepo.UpdateWorkflow(workflow);
              UnitOfWork.AuditTrialRepo.Add(auditTrail);
              UnitOfWork.Complete();
              errorMessage = "";
              return true;
            }
            errorMessage = "Invalid Workflow id";
            return false;
          }
          if(entity.Action ==  nameof(TempTableAction.Reactivate).Replace("_", " "))
          {
            if (entity.WorkflowId != null)
            {
              var workflow = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
              if (workflow == null)
              {
                errorMessage = "Invalid workflow id";
                return false;
              }

              var auditTrail = new TblAuditTrail
              {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                NewFieldValue = $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                                $"Date: {entity.Date}, " +
                                $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                                $"TransactionType: {entity.TransactionType}",
                TransactionId = "",
                UserId = BankProfile.Id,
                Username = UserName,
                Description = $"Approved WorkFlow Reactivation. Action was carried out by a Bank user",
                TimeStamp = DateTime.Now
              };

              entity.IsTreated = (int)ProfileStatus.Active;
              workflow.Status = (int)ProfileStatus.Active;
              entity.Status = (int)ProfileStatus.Active;
              entity.ApprovedId = BankProfile.Id;
              entity.ApprovalUsername = UserName;
              entity.ActionResponseDate = DateTime.Now;
              entity.Reasons = payload.Reason;
              UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
              UnitOfWork.WorkFlowRepo.UpdateWorkflow(workflow);
              UnitOfWork.AuditTrialRepo.Add(auditTrail);
              UnitOfWork.Complete();
              errorMessage = "";
              return true;
            }
            errorMessage = "Invalid Workflow id";
            return false;
          }
          errorMessage = "Unknown Request";
          return false;
        }
        private bool RequestApproval(TblTempWorkflow entity, SimpleAction payload, out string errorMessage)
        {
            if (entity.CorporateCustomerId != null)
            {
                var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
                if (corporateCustomer == null)
                {
                    errorMessage = "Invalid Corporate Customer id";
                    return false;
                }
                var notifyInfo = new EmailNotification
                {
                    CustomerId = corporateCustomer.CustomerId,
                    WorkflowName = entity.Name,
                    Description = entity.Description,
                    NoOfAuthorizers = entity.NoOfAuthorizers,
                    ApprovalLimit = entity.ApprovalLimit
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
                        ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
                        Ipaddress = payload.IPAddress,
                        Macaddress = payload.MACAddress,
                        HostName = payload.HostName,
                        NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                        $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                        $"TransactionTypet: {entity.TransactionType}",
                        PreviousFieldValue ="",
                        TransactionId = "",
                        UserId = BankProfile.Id,
                        Username = UserName,
                        Description = "Request Workflow Approval for newly created Workflow",
                        TimeStamp = DateTime.Now
                    };
                    
                    //update status
                    entity.Status = (int) ProfileStatus.Pending;
                    UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                    UnitOfWork.AuditTrialRepo.Add(auditTrail);
                    UnitOfWork.Complete();
                    //notify.NotifyBankAdminAuthorizer(entity,true, payload.Reason);
                    notify.NotifyBankAdminAuthorizerForCorporateWorkflowApproval(notifyInfo);
                    errorMessage = "Request Approval Was Successful";
                    return true;
                }
                
                if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
                {
                    if (entity.WorkflowId != null)
                    {
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
                            NewFieldValue = $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                                $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                                $"TransactionType: {entity.TransactionType}",
                            PreviousFieldValue ="",
                            TransactionId = "",
                            UserId = BankProfile.Id,
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
                        notify.NotifyBankAdminAuthorizerForCorporateWorkflowApproval(notifyInfo);
                        errorMessage = "Request Approval Was Successful";
                        return true;
                    }
                    errorMessage = "Invalid WorkFlow id";
                    return false;
                }
                errorMessage = "invalid Request";
                return false;
            }  
            errorMessage = "invalid Corporate Customer Id";
            return false;
        }
        private bool DeclineRequest(TblTempWorkflow entity, AppActionDto payload, out string errorMessage)
        {
            var initiatorProfile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);

            var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
            
            if (corporateCustomer == null)
            {
              errorMessage = "Invalid Corporate Customer";
              return false;
            }
            
            var notifyInfo = new EmailNotification
            {
                CustomerId = corporateCustomer.CustomerId,
                WorkflowName = entity.Name,
                Description = entity.Description,
                NoOfAuthorizers = entity.NoOfAuthorizers,
                ApprovalLimit = entity.ApprovalLimit,
                Action = entity.Action,
                Reason = entity.Reasons,
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
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Decline Workflow",
                    TimeStamp = DateTime.Now
                };

                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Declined;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = BankProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                notify.NotifyBankAdminAuthorizerForCorporateWorkflowDecline(initiatorProfile, notifyInfo);
                errorMessage = "Decline Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
            {
                
                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                if (profile == null)
                {
                    errorMessage = "Invalid Work Flow id";
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
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Decline Workflow",
                    TimeStamp = DateTime.Now
                };

            
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                var originalStatus = profile.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int) entity.PreviousStatus;

                entity.Status = (int) ProfileStatus.Declined;
                profile.Status = originalStatus;
                entity.IsTreated = (int)ProfileStatus.Declined;
                profile.ReasonForDeclining = entity.Reasons;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = BankProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                //notify.NotifyBankMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                notify.NotifyBankAdminAuthorizerForCorporateWorkflowDecline(initiatorProfile, notifyInfo);
                errorMessage = "Decline Approval Was Successful";
                return true;
            }
            
            if(entity.Action ==  nameof(TempTableAction.Deactivate).Replace("_", " "))
            {
                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                if (profile == null)
                {
                    errorMessage = "Invalid Bank Profile id";
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
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Decline Workflow",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                //entity.Status = (int) ProfileStatus.Declined;
                //profile.Status = (int) ProfileStatus.Active;
                entity.Status = (int)ProfileStatus.Declined;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = entity.Reasons;
                entity.ApprovedId = BankProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                //notify.NotifyBankMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                notify.NotifyBankAdminAuthorizerForCorporateWorkflowDecline(initiatorProfile, notifyInfo);
                errorMessage = "Decline Approval Was Successful";
                return true;
            }
        
            if(entity.Action ==  nameof(TempTableAction.Reactivate).Replace("_", " "))
            {
                var profile = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)entity.WorkflowId);
                if (profile == null)
                {
                    errorMessage = "Invalid Workflow Id";
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
                    ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Decline Workflow",
                    TimeStamp = DateTime.Now
                };
                
                //update status
                //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
                entity.Status = (int) ProfileStatus.Declined;
                profile.ReasonForDeclining = entity.Reasons;
                entity.IsTreated = (int) ProfileStatus.Declined;
                entity.Reasons = payload.Reason;
                entity.ApprovedId = BankProfile.Id;
                entity.ApprovalUsername = UserName;
                entity.ActionResponseDate = DateTime.Now;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(entity);
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(profile);
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.Complete();
                //notify.NotifyBankMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
                notify.NotifyBankAdminAuthorizerForCorporateWorkflowDecline(initiatorProfile, notifyInfo);
                errorMessage = "Decline Request Was Successful";
                return true;
            }
            
            errorMessage = "invalid Request";
            return false;
        }  
    }
}
