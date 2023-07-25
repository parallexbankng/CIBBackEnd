using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.BankAdmin.Controllers
{
    [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class AuditTrialController : BaseAPIController
    {
        private readonly ILogger<AuditTrialController> _logger;
        public AuditTrialController(ILogger<AuditTrialController> _logger,IUnitOfWork unitOfWork,IMapper mapper,IHttpContextAccessor accessor,IAuthenticationService authService):base(mapper,unitOfWork,accessor,authService)
        {
            this._logger = _logger;
        }
        
        [HttpGet("GetAuditActions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<TransactionTypeModel>> GetAuditActions()
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

                List<TransactionTypeModel> auditActions = new List<TransactionTypeModel>();
                var enums = Enum.GetValues(typeof(AuditTrailAction)).Cast<AuditTrailAction>().ToList();
                foreach (var e in enums)
                {
                    auditActions.Add(new TransactionTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
                }

                return Ok(auditActions);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return Ok(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }
        
        [HttpGet("GetAuditTrails")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<List<TblAuditTrail>> Search(string userId = null, string userName = null, string action = null, string dateFrom = null, string dateTo = null)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewAuditTrail))
                {
                    return BadRequest("UnAuthorized Access");
                }

                Guid? _userId = null;
                var UserName = "";
                var Action = "";
                if (!string.IsNullOrEmpty(userId))
                {
                    _userId = Encryption.DecryptGuid(userId);
                }

                if (!string.IsNullOrEmpty(userName))
                {
                    UserName = Encryption.DecryptStrings(userName);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    Action = Encryption.DecryptStrings(action);
                }

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewAuditTrail))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var _dateFrom = new DateTime();
                var _dateTo = new DateTime();
                if(!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo))
                {
                    _dateFrom = Encryption.DecryptDateTime(dateFrom);
                    _dateTo = Encryption.DecryptDateTime(dateTo);
                    if (!DateTime.TryParse(_dateFrom.ToString(), out _dateFrom) || !DateTime.TryParse(_dateTo.ToString(), out _dateTo))
                    {
                        return BadRequest("Please enter a valid start and end date");
                    }
                }

                var auditTrails = UnitOfWork.AuditTrialRepo.Search(_userId, UserName, Action?.Replace("_"," "), _dateFrom, _dateTo)?.ToList();
                if (auditTrails == null || auditTrails?.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(auditTrails);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }
   
        [HttpGet("TransactionReports")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult<List<TblTransaction>> TransactionReport(string CorporateCustomerId, string Transref, string dateFrom = null, string dateTo = null, string IsBulk = null)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewAuditTrail))
                {
                    return BadRequest("UnAuthorized Access");
                }
                Guid? _userId = null;
                var UserName = "";
                bool _IsBulk = false;
                if (!string.IsNullOrEmpty(CorporateCustomerId))
                {
                    _userId = Encryption.DecryptGuid(CorporateCustomerId);
                }
                if (!string.IsNullOrEmpty(IsBulk))
                {
                    _IsBulk = Encryption.DecryptBooleans(IsBulk);
                }

                if (!string.IsNullOrEmpty(Transref))
                {
                    UserName = Encryption.DecryptStrings(Transref);
                }
                var _dateFrom = new DateTime();
                var _dateTo = new DateTime();
                if(!string.IsNullOrEmpty(dateFrom) && !string.IsNullOrEmpty(dateTo))
                {
                    _dateFrom = Encryption.DecryptDateTime(dateFrom);
                    _dateTo = Encryption.DecryptDateTime(dateTo);
                    if (!DateTime.TryParse(_dateFrom.ToString(), out _dateFrom) || !DateTime.TryParse(_dateTo.ToString(), out _dateTo))
                    {
                        return BadRequest("Please enter a valid start and end date");
                    }
                }
                var allTransaction = _unitOfWork.TransactionRepo.Search(_userId, UserName, _dateFrom, _dateTo,_IsBulk)?.ToList();
                if (allTransaction == null || allTransaction?.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(allTransaction);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpGet("BulkCreditLogs")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<List<TblNipbulkTransferLog>>> BulkCreditLogs(string bulkFileId)
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

                if (string.IsNullOrEmpty(bulkFileId))
                {
                    return BadRequest("Bulk file Id is required");
                }

                var Id = Encryption.DecryptGuid(bulkFileId);
                var tblTransactions = UnitOfWork.NipBulkCreditLogRepo.GetbulkCreditLog(Id);
                return Ok(tblTransactions);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }
    }
}
