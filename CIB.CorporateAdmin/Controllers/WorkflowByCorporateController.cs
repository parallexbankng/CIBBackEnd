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
using CIB.Core.Modules.Workflow.Dto;
using CIB.Core.Modules.Workflow.Validation;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class WorkflowByCorporateController : BaseAPIController
    {
        private readonly ILogger<WorkflowByCorporateController> _logger;
        public WorkflowByCorporateController(ILogger<WorkflowByCorporateController> logger,IUnitOfWork unitOfWork, IMapper mapper,IHttpContextAccessor accessor,IAuthenticationService authService):base(unitOfWork,mapper,accessor,authService)
        {
            _logger = logger;
        }

        [HttpGet("GetWorkflow")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<TblWorkflow> GetWorkflow()
        {
            if (!IsAuthenticated)
            {
                return StatusCode(401, "User is not authenticated");
            }

            if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanViewWorkflowByCorporateAdmin))
            {
                return BadRequest("UnAuthorized Access");
            }

            if(CorporateProfile == null)
            {
                return BadRequest("Invalid corporate id");
            }
            
            var Workflow = UnitOfWork.WorkFlowRepo.GetWorkflowByCorporateCustomerId((Guid)CorporateProfile.CorporateCustomerId);
            if (Workflow == null)
            {
                return BadRequest("Invalid corporate customer id. Work flow not found");
            }
            return Ok(Workflow);
        }

        [HttpPost("CreateWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<TblWorkflow> CreateWorkflow(CreateCorporateWorkflow model)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateWorkflowByCorporateAdmin))
                {
                    return BadRequest("UnAuthorized Access");
                }
                if (model == null){
                    return BadRequest("invalid request");
                }
                var payload = new CreateCorporateWorkflowDto
                {
                    Name = Encryption.DecryptStrings(model.Name),
                    Description = Encryption.DecryptStrings(model.Name),
                    Date = Encryption.DecryptDateTime(model.Date),
                    NoOfAuthorizers = Encryption.DecryptInt(model.NoOfAuthorizers),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    TransactionType = Encryption.DecryptStrings(model.Name),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };

                var validator = new CreateCorporateWorkFlowValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                if (CorporateProfile == null)
                {
                    return BadRequest("Invalid corporate id");
                }

            // TblWorkflow entity = Factory.Workflows.Create(model.Name, model.Description, model.TransactionType, (Guid)corporateCustomerId, model.NoOfAuthorizers);
                if (payload.WorkflowHierarchies == null || payload.WorkflowHierarchies?.Count == 0)
                {
                    return BadRequest("Workflow Hierarchies is require");
                }
                var tblWorkflowHierarchies = new List<TblWorkflowHierarchy>();
                var newCorporateWorkFlow = new TblWorkflow
                {
                    Id = Guid.NewGuid(),
                    Name = payload.Name,
                    Description = payload.Description,
                    TransactionType = payload.TransactionType,
                    CorporateCustomerId = payload.CorporateCustomerId,
                    NoOfAuthorizers = payload.NoOfAuthorizers,
                    Status = 0
                };
                foreach(var item in payload.WorkflowHierarchies)
                {
                    var workFlowUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync(item.ApproverId);
                    var corporateUserRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(item.RoleId);
                    if (workFlowUser == null)
                    {
                        return BadRequest("Approver Id is invalid");
                    }
                    if (workFlowUser.ApprovalLimit < payload.ApprovalLimit)
                    {
                        decimal appLimit = (decimal)payload.ApprovalLimit;
                        decimal formattedAppLimit = decimal.Round(appLimit, 2, MidpointRounding.AwayFromZero);
                        decimal corpAppLimit = (decimal)workFlowUser.ApprovalLimit;
                        decimal corpFormattedAppLimit = decimal.Round(corpAppLimit, 2, MidpointRounding.AwayFromZero);
                        string fullName = workFlowUser.FirstName + " " + workFlowUser.LastName;
                        return BadRequest($"Workflow approval limit {formattedAppLimit} is higher than {fullName.ToUpper()} approval limit {corpFormattedAppLimit}");
                    }

                    //var tblWorkflowHierarchy = Factory.WorkflowHierarchies.Create(item.AccountLimit, item.ApproverID,theCorporateProfile.FullName, item.AuthorizationLevel, item.RoleID, theCorporateRole.RoleName, model.Id.ToString());
                    var tblWorkflowHierarchy = new TblWorkflowHierarchy
                    {
                        Id = Guid.NewGuid(),
                        AccountLimit = payload.ApprovalLimit,
                        ApproverId = (Guid)item.ApproverId,
                        ApproverName = workFlowUser.FullName,
                        AuthorizationLevel = item.AuthorizationLevel,
                        RoleId = (Guid)item.RoleId,
                        RoleName = corporateUserRole.RoleName,
                        WorkflowId = newCorporateWorkFlow.Id
                    };
                    tblWorkflowHierarchies.Add(tblWorkflowHierarchy);
                }
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress = payload.ClientStaffIPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Workflow Name: {payload.Name}, Description: {payload.Description}, " +
                    $"Date: {payload.Date}, " + $"No Of Authorizers: {payload.NoOfAuthorizers}, CorporateCustomerId: {payload.CorporateCustomerId}, ApprovalLimit: {payload.ApprovalLimit}, " +
                    $"TransactionTypet: {payload.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Corporate User Create Workflow",
                    TimeStamp = DateTime.Now
                };

                UnitOfWork.WorkFlowRepo.Add(newCorporateWorkFlow);
                UnitOfWork.WorkFlowHierarchyRepo.AddRange(tblWorkflowHierarchies);
                UnitOfWork.Complete();
                return CreatedAtRoute(new { id = newCorporateWorkFlow.Id }, newCorporateWorkFlow);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("UpdateWorkflow")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<TblWorkflow> UpdateWorkflow(UpdateWorkflow model)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanUpdateWorkflowByCorporateAdmin))
                {
                    return BadRequest("UnAuthorized Access");
                }

                if (CorporateProfile == null)
                {
                    return BadRequest("Invalid corporate id");
                }
                var payload = new UpdateWorkflowDto
                {
                    Name = Encryption.DecryptStrings(model.Name),
                    Description = Encryption.DecryptStrings(model.Description),
                    TransactionType = Encryption.DecryptStrings(model.TransactionType),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    NoOfAuthorizers = Encryption.DecryptInt(model.NoOfAuthorizers),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)

                };
                //get profile by username
                var entity = UnitOfWork.WorkFlowRepo.GetWorkflowByID((Guid)CorporateProfile.CorporateCustomerId);
                if (entity == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                var audit = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =   $"Workflow Name: {payload.Name}, Description: {payload.Description}, " +
                    $"No Of Authorizers: {payload.NoOfAuthorizers}, CorporateCustomerId: {payload.CorporateCustomerId}," +
                    $"TransactionType: {payload.TransactionType}",
                    PreviousFieldValue =$"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = CorporateProfile.Username,
                    Description = "Corporate User Update Work Flow",
                    TimeStamp = DateTime.Now
                };
                

                //update existing info
                entity.Name = payload.Name;
                entity.Description = payload.Description;
                entity.TransactionType = payload.TransactionType;
                entity.CorporateCustomerId = payload.CorporateCustomerId;
                entity.NoOfAuthorizers = payload.NoOfAuthorizers;
                UnitOfWork.AuditTrialRepo.Add(audit);
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(entity);
                UnitOfWork.Complete();
                return CreatedAtRoute(new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("RequestWorkflowApproval")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<TblWorkflow> RequestWorkflowApproval(SimpleActionDto model)
        {
            try
            {
                if (!IsAuthenticated)
                {
                    return StatusCode(401, "User is not authenticated");
                }

                if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanRequestWorkflowApprovalByCorporateAdmin))
                {
                    return BadRequest("UnAuthorized Access");
                }
                //var workflowId = Encryption.DecryptGuid(id);
                var payload = new SimpleAction
                {
                    Id = Encryption.DecryptGuid(model.Id),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress)
                };
                var entity = UnitOfWork.WorkFlowRepo.GetWorkflowByID(payload.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid ID");
                }

                if (CorporateProfile == null)
                {
                    return BadRequest("Invalid corporate id");
                }

                if (entity.CorporateCustomerId != CorporateProfile.CorporateCustomerId)
                {
                    return BadRequest("Invalid Id");
                }

                if (entity.Status != 2)
                {
                    return BadRequest("Workflow approval cannot be requested as it is was not declined");
                }
                
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    NewFieldValue =   $"Workflow Name: {entity.Name}, Description: {entity.Description}, " +
                    $"Date: {entity.Date}, " + $"No Of Authorizers: {entity.NoOfAuthorizers}, CorporateCustomerId: {entity.CorporateCustomerId}, ApprovalLimit: {entity.ApprovalLimit}, " +
                    $"TransactionTypet: {entity.TransactionType}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = "Requested Workflow Approval by Corporate User",
                    TimeStamp = DateTime.Now
                };

                //update info
                entity.Status = 1;
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(entity);
                UnitOfWork.Complete();
                return CreatedAtRoute(new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        protected  bool ValidateWorkflowDropDowns(string approverId, out TblCorporateProfile theCorporateProfile, out string errormsg, TblWorkflow tblWorkflow)
        {
            errormsg = string.Empty;
            theCorporateProfile = UnitOfWork.CorporateProfileRepo.GetProfileByID(Guid.Parse(approverId));
            if (theCorporateProfile == null)
            {
                errormsg = "Approver Id is invalid";
                return false;
            }
            if (theCorporateProfile.ApprovalLimit < tblWorkflow.ApprovalLimit)
            {
                decimal appLimit = (decimal)tblWorkflow.ApprovalLimit;
                decimal formattedAppLimit = decimal.Round(appLimit, 2, MidpointRounding.AwayFromZero);

                decimal corpAppLimit = (decimal)theCorporateProfile.ApprovalLimit;
                decimal corpFormattedAppLimit = decimal.Round(corpAppLimit, 2, MidpointRounding.AwayFromZero);

                string fullName = theCorporateProfile.FirstName + " " + theCorporateProfile.LastName;

                errormsg = $"Workflow approval limit {formattedAppLimit} is higher than {fullName.ToUpper()} approval limit {corpFormattedAppLimit}";
                return false;
            }
            return true;
        }
    }
}