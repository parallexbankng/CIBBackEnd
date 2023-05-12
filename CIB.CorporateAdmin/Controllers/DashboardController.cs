using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Enums;
using CIB.Core.Services.Api;
using CIB.Core.Utils;
using CIB.CorporateAdmin.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class DashboardController : BaseAPIController
    {
        private readonly IApiService _apiService;
        private readonly ILogger<CorporateRoleController> _logger;
        public DashboardController(ILogger<CorporateRoleController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IApiService apiService) : base( unitOfWork,mapper,accessor)
        {
            _apiService = apiService;
            _logger = logger;
        }
        [HttpGet("GetDashboardInfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<DashboardModel>> GetDashboardInfo()
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

                if (CorporateProfile.CorporateCustomerId != null)
                {
                  var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                  if (tblCorporateCustomer == null)
                  {
                    return BadRequest("Invalid corporate customer id");
                  }
                
                  if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.ViewCorporateAccount))
                  {
                    if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType authType))
                    {
                      if (authType != AuthorizationType.Single_Signatory)
                      {
                        return BadRequest("UnAuthorized Access");
                      }
                    }
                    else
                    {
                      return BadRequest("Authorization type could not be determined!!!");
                    }
                  }

                  //get transactions
                  var pendingTranLogs = UnitOfWork.PendingTranLogRepo.GetAllCompanySingleTransactionInfo(tblCorporateCustomer.Id);
     
                  var bulkTransactionInfo = UnitOfWork.NipBulkTransferLogRepo.GetBulkPendingTransferLog(tblCorporateCustomer.Id);

                  var transactionInfo = UnitOfWork.TransactionRepo.GetCorporateTransactionReport(tblCorporateCustomer.Id);

                  int pendingTrans = 0;
                  int failedTrans = 0;
                  int successfulTrans = 0;
                  int totalTrans = 0;
                  int bulkPending = 0;
                  int singlePending = 0;
                  //int declineTrans = 0;

                  if(transactionInfo != null)
                  {
                    failedTrans = transactionInfo?.Count(x => x.TransactionStatus == "Failed") ?? 0;
                    successfulTrans = transactionInfo?.Count(x => x.TransactionStatus == "Successful") ?? 0;
                    totalTrans = transactionInfo?.Count() ?? 0;
                  }

                  if(pendingTranLogs != null)
                  {
                    singlePending = pendingTranLogs?.Count() ?? 0;
                  }

                  if(bulkTransactionInfo != null)
                  {
                    bulkPending = bulkTransactionInfo?.Count ?? 0;
                   
                  }

                  pendingTrans = bulkPending + singlePending;
                  var dto = await  _apiService.RelatedCustomerAccountDetails(tblCorporateCustomer.CustomerId);

                  var dashboard = new DashboardModel()
                  {
                    Accounts = dto.Records,
                    PendingTransactions = pendingTrans,
                    TotalTransactions = totalTrans,
                    FailedTransaction = failedTrans,
                    SuccessfulTransactions = successfulTrans
                  };
                  return Ok(dashboard);
                }
                return BadRequest("Corporate customer Id could not be retrieved");
            }
            catch (Exception ex)
            {
              _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
              return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
    }
}
