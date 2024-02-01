using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Services.Api;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using CIB.CorporateAdmin.Dto;
using CIB.CorporateAdmin.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class DashboardController : BaseAPIController
	{
		private readonly IApiService _apiService;
		private readonly ILogger<CorporateRoleController> _logger;
		public DashboardController(ILogger<CorporateRoleController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IApiService apiService, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
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
					// var pendingTranLogs = UnitOfWork.PendingTranLogRepo.GetAllCompanySingleTransactionInfo(tblCorporateCustomer.Id);

					// var bulkTransactionInfo = UnitOfWork.NipBulkTransferLogRepo.GetBulkPendingTransferLog(tblCorporateCustomer.Id);

					// var transactionInfo = UnitOfWork.TransactionRepo.GetCorporateTransactionReport(tblCorporateCustomer.Id);

					var workFlows = UnitOfWork.WorkFlowRepo.GetActiveWorkflow(tblCorporateCustomer.Id).ToList();
					var accountNumbers = new List<RelatedCustomerAccountDetail>();
					var getAggregateAccount = UnitOfWork.CorporateAggregationRepo.GetCorporateCustomerAggregations(CorporateProfile.CorporateCustomerId);
					if (getAggregateAccount.Any())
					{
						var dtoo = await _apiService.RelatedCustomerAccountDetails(tblCorporateCustomer.CustomerId);
						if (dtoo.RespondCode != "00")
						{
							LogFormater<CorporateRoleController>.Error(_logger, "Get Corporate Accounts", dtoo.RespondMessage, JsonConvert.SerializeObject(tblCorporateCustomer.CustomerId), "");
							return BadRequest(dtoo.RespondMessage);
						}
						accountNumbers.AddRange(dtoo?.Records);
						var relatedAccountDetails = await GetRelatedAccountNumber(getAggregateAccount);
						if (relatedAccountDetails.Any())
						{
							accountNumbers.AddRange(relatedAccountDetails);
						}
					}
					else
					{
						var result = await _apiService.RelatedCustomerAccountDetails(tblCorporateCustomer.CustomerId);
						if (result.RespondCode != "00")
						{
							return BadRequest(result.RespondMessage);
						}
						accountNumbers.AddRange(result.Records);
					}

					var dashboard = new DashboardModel
					{
						Accounts = accountNumbers,
						Workflows = workFlows
					};
					return Ok(dashboard);
				}
				return BadRequest("Corporate customer Id could not be retrieved");
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		private async Task<List<RelatedCustomerAccountDetail>> GetRelatedAccountNumber(List<TblCorporateAccountAggregation>? aggregateAccount)
		{
			var accountList = new List<RelatedCustomerAccountDetail>();
			if (aggregateAccount.Any())
			{
				foreach (var account in aggregateAccount)
				{
					var result = await _apiService.RelatedCustomerAccountDetails(account.CustomerId);
					if (result.RespondCode == "00")
					{
						var addedRelatedAccount = UnitOfWork.AggregatedAccountRepo.GetCorporateAggregationAccountByAggregateId(account.Id);
						if (addedRelatedAccount.Any())
						{
							foreach (var returnAccount in addedRelatedAccount)
							{
								var resultAccount = result?.Records?.FirstOrDefault(ctx => ctx.AccountNumber == returnAccount.AccountNumber);
								if (resultAccount != null)
								{
									accountList.Add(resultAccount);
								}
							}
						}
					}
				}
			}
			return accountList;
		}
	}
}
