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
using CIB.Core.Modules.WorkflowHierarchy.Dto;
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
    public class WorkFlowHierarchyController : BaseAPIController
    {
        private readonly ILogger<WorkFlowHierarchyController> _logger;
        public WorkFlowHierarchyController(ILogger<WorkFlowHierarchyController> _logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
        {
            this._logger = _logger;
        }

        [HttpGet("GetWorkflowHierarchy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ResponseDTO<WorkflowHierarchyResponseDto>> GetWorkflowHierarchy(string id)
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
                var workflowId = Encryption.DecryptGuid(id);
                var WorkflowHierarchy = UnitOfWork.WorkFlowHierarchyRepo.GetByIdAsync(workflowId);
                if (WorkflowHierarchy == null)
                {
                    return BadRequest("Invalid id. Workflow hierarchy not found");
                }
                return Ok(new ResponseDTO<WorkflowHierarchyResponseDto>(_data:Mapper.Map<WorkflowHierarchyResponseDto>(WorkflowHierarchy),success:true, _message:Message.Success) );
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

        [HttpGet("GetWorkflowHierarchies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ResponseDTO<List<WorkflowHierarchyResponseDto>>> GetWorkflowHierarchiesByWorkflowID(string workflowId)
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
                var id = Encryption.DecryptStrings(workflowId);
                var WorkflowHierarchy = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(Guid.Parse(id));
                return Ok(new ListResponseDTO<WorkflowHierarchyResponseDto>(_data:Mapper.Map<List<WorkflowHierarchyResponseDto>>(WorkflowHierarchy),success:true, _message:Message.Success) );
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

        [HttpGet("GetTempWorkflowHierarchies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<ResponseDTO<List<WorkflowHierarchyResponseDto>>> GetTempWorkflowHierarchies(string workflowId)
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
                var id = Encryption.DecryptStrings(workflowId);
                var WorkflowHierarchy = UnitOfWork.TempWorkflowHierarchyRepo.GetTempWorkflowHierarchyByWorkflowId(Guid.Parse(id));
                return Ok(new ListResponseDTO<TblTempWorkflowHierarchy>(_data:Mapper.Map<List<TblTempWorkflowHierarchy>>(WorkflowHierarchy),success:true, _message:Message.Success) );
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

        [HttpPost("CreateWorkflowHierarchies")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkflowHierarchyResponseDto>> CreateWorkflowHierarchies(List<CreateWorkflowHierarchy> model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
                {
                 return BadRequest("UnAuthorized Access");
                }
                //validate workflow id
                var data = model.FirstOrDefault();
                var payload = new SimpleActionDto
                {
                    IPAddress = Encryption.DecryptStrings(data.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(data.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(data.HostName),
                    MACAddress = Encryption.DecryptStrings(data.MACAddress)
                };
                var workflowId = Encryption.DecryptGuid(model.FirstOrDefault().WorkflowId);
                var tblWorkflow = UnitOfWork.TempWorkflowRepo.GetByIdAsync(workflowId);

                if(tblWorkflow == null)
                {
                    return BadRequest("Invalid workflow id. Workflow info not found");
                }

                if(tblWorkflow.NoOfAuthorizers != model.Count)
                {
                    return BadRequest("Number of authorizers specified in workflow must match the number of authorizers added");
                }

                if (tblWorkflow.Status == (int)ProfileStatus.Pending)
                {
                    return BadRequest("A pending approval has been detected. Update is not permitted until approval is completed");
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)tblWorkflow.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Corporate customer was not found");
                }

                var tblWorkflowHierarchies = new List<TblTempWorkflowHierarchy>();
                foreach (var item in model)
                {
                    var approvalId = Encryption.DecryptGuid(item.ApproverId);
                    var approvalName = Encryption.DecryptStrings(item.ApproverName);

                    var corporateProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync(approvalId);
                    if(corporateProfile == null)
                    {
                       return BadRequest($"Approver {approvalName} was not found");
                    }

                    var corporateRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)corporateProfile.CorporateRole);
                    if(corporateRole == null)
                    {
                       return BadRequest($"Approver {corporateRole.RoleName} was not found");
                    }

                    if(corporateRole.RoleName != nameof(UserRole.Corporate_Authorizer).Replace("_", " ") )
                    {
                       return BadRequest($"Only Corporate Authorizer can be added to workflow");
                    }
                    
                    
                    var newWorkFlow = new TblTempWorkflowHierarchy
                    {
                        Id = Guid.NewGuid(),
                        WorkflowId = workflowId,
                        AuthorizationLevel = Encryption.DecryptInt(item.AuthorizationLevel),
                        ApproverId = approvalId,
                        ApproverName = approvalName,
                        RoleId = corporateRole.Id,
                        RoleName = corporateRole.RoleName
                    };

                    if (!ValidateWorkflowDropDowns(corporateProfile,tblWorkflow,out errormsg))
                    {
                        return BadRequest(errormsg);
                    }
                    tblWorkflowHierarchies.Add(newWorkFlow);
                }
                
                //var toBeDeleted = UnitOfWork.WorkFlowHierarchyRepo.GetByIdAsync(tblWorkflow.Id);
                string newRecords = string.Empty;
                string oldRecords = string.Empty;
                var deleteWorkflowHierarchiesList = UnitOfWork.TempWorkflowHierarchyRepo.GetTempWorkflowHierarchyByWorkflowId(tblWorkflow.Id).ToList();

                if(deleteWorkflowHierarchiesList?.Count != 0 && deleteWorkflowHierarchiesList != null)
                {
                    foreach (var item in deleteWorkflowHierarchiesList)
                    {
                        oldRecords += $"Approver: {item.ApproverName}, Level: {item.AuthorizationLevel} \n";
                    }
                    UnitOfWork.TempWorkflowHierarchyRepo.RemoveRange(deleteWorkflowHierarchiesList);
                }

                foreach(var item in tblWorkflowHierarchies)
                {
                    newRecords += $"Approver: {item.ApproverName}, Level: {item.AuthorizationLevel} \n";
                }

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Company Name: {tblCorporateCustomer.CompanyName}, Customer ID: {tblCorporateCustomer.CustomerId}, Workflow Name: {tblWorkflow.Name}, Approvers: {newRecords}, Status: {ProfileStatus.Modified.ToString()}",
                    PreviousFieldValue =$"Company Name: {tblCorporateCustomer.CompanyName}, Customer ID: {tblCorporateCustomer.CustomerId},Workflow Name: {tblWorkflow.Name}, Approvers: {oldRecords}, Status: {ProfileStatus.Modified.ToString()}",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = "Create/Update Workflow Hierarch by Bank Admin User Access",
                    TimeStamp = DateTime.Now
                };
    
                tblWorkflow.Status = (int)ProfileStatus.Modified;
                UnitOfWork.TempWorkflowRepo.UpdateTempWorkflow(tblWorkflow);
                UnitOfWork.TempWorkflowHierarchyRepo.AddRange(tblWorkflowHierarchies);
                UnitOfWork.Complete();
                return CreatedAtRoute("", tblWorkflowHierarchies);
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
    
        [HttpPost("ModifyWorkflowHierarchies")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<WorkflowHierarchyResponseDto>> UpdateWorkflowHierarchies(List<CreateWorkflowHierarchy> model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewWorkflow))
                {
                 return BadRequest("UnAuthorized Access");
                }
                //validate workflow id
                var data = model.FirstOrDefault();
                var payload = new SimpleActionDto
                {
                    IPAddress = Encryption.DecryptStrings(data.IPAddress),
                    ClientStaffIPAddress = Encryption.DecryptStrings(data.ClientStaffIPAddress),
                    HostName = Encryption.DecryptStrings(data.HostName),
                    MACAddress = Encryption.DecryptStrings(data.MACAddress)
                };
                var workflowId = Encryption.DecryptGuid(model.FirstOrDefault().WorkflowId);
                var tblWorkflow = UnitOfWork.WorkFlowRepo.GetByIdAsync(workflowId);

                if(tblWorkflow == null)
                {
                    return BadRequest("Invalid workflow id. Workflow info not found");
                }

                if (tblWorkflow.Status == (int)ProfileStatus.Pending || tblWorkflow.Status == (int)ProfileStatus.Modified)
                {
                    return BadRequest("There is either a pending modification or approval for this workflow");
                }

                if(tblWorkflow.NoOfAuthorizers != model.Count)
                {
                    return BadRequest("Number of authorizers specified in workflow must match the number of authorizers added");
                }
                
                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)tblWorkflow.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Corporate customer was not found");
                }
                var mapTempWorkFlow = Mapper.Map<TblTempWorkflow>(tblWorkflow);
                mapTempWorkFlow.Id = Guid.NewGuid();

                var tblWorkflowHierarchies = new List<TblTempWorkflowHierarchy>();
                foreach (var item in model)
                {
                    var approvalId = Encryption.DecryptGuid(item.ApproverId);
                    var approvalName = Encryption.DecryptStrings(item.ApproverName);

                    var corporateProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync(approvalId);
                    if(corporateProfile == null)
                    {
                       return BadRequest($"Approver {approvalName} was not found");
                    }
                    var corporateRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)corporateProfile.CorporateRole);
                    if(corporateRole == null)
                    {
                       return BadRequest($"Approver {corporateRole.RoleName} was not found");
                    }

                    if(corporateRole.RoleName != nameof(UserRole.Corporate_Authorizer).Replace("_", " ") )
                    {
                       return BadRequest($"Only Corporate Authorizer can be added to workflow");
                    }
                    
                    var newWorkFlow = new TblTempWorkflowHierarchy
                    {
                        Id = Guid.NewGuid(),
                        Sn = 0,
                        WorkflowId = mapTempWorkFlow.Id,
                        AuthorizationLevel = Encryption.DecryptInt(item.AuthorizationLevel),
                        ApproverId = approvalId,
                        ApproverName = approvalName,
                        RoleId = corporateRole.Id,
                        RoleName = corporateRole.RoleName
                    };

                   
                    if (!ValidateWorkflowDropDowns(corporateProfile,tblWorkflow,out errormsg))
                    {
                        return BadRequest(errormsg);
                    }

                    if(tblWorkflowHierarchies.Any(ctx => ctx.ApproverId.ToString() == item.ApproverId))
                    {
                        return BadRequest($"Approver {corporateProfile.FullName} has been added multiple times. Please reactify");
                    }
                    tblWorkflowHierarchies.Add(newWorkFlow);
                }

                mapTempWorkFlow.Sn = 0;
                mapTempWorkFlow.Status =(int)ProfileStatus.Modified;
                mapTempWorkFlow.WorkflowId = tblWorkflow.Id;
                mapTempWorkFlow.DateRequested = DateTime.Now;
                mapTempWorkFlow.Action = nameof(TempTableAction.Update).Replace("_", " ");
                mapTempWorkFlow.InitiatorId = BankProfile.Id;
                mapTempWorkFlow.InitiatorUsername = UserName;
                
                tblWorkflow.Status = (int)ProfileStatus.Modified;
                mapTempWorkFlow.IsTreated = (int)ProfileStatus.Pending;
                UnitOfWork.WorkFlowRepo.UpdateWorkflow(tblWorkflow);
                UnitOfWork.TempWorkflowRepo.Add(mapTempWorkFlow);
                UnitOfWork.TempWorkflowHierarchyRepo.AddRange(tblWorkflowHierarchies);
                UnitOfWork.Complete();
                return CreatedAtRoute("", tblWorkflowHierarchies);
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
    
    

        public static bool ValidateWorkflowDropDowns(TblCorporateProfile theCorporateProfile,TblTempWorkflow tblWorkflow, out string errormsg)
        {
            errormsg = string.Empty;
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

        public static bool ValidateWorkflowDropDowns(TblCorporateProfile theCorporateProfile,TblWorkflow tblWorkflow, out string errormsg)
        {
            errormsg = string.Empty;
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