
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Enums;
using CIB.Core.Exceptions;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using CIB.Core.Modules.OnLending.Enums;
using CIB.Core.Modules.OnLending.Liquidation.Dto;
using CIB.Core.Modules.OnLending.Liquidation.Enum;
using CIB.Core.Services._2FA;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Services.Notification;
using CIB.Core.Services.OnlendingApi;
using CIB.Core.Services.OnlendingApi.Dto;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CIB.CorporateAdmin.Controllers
{
  [ApiController]
  [Route("api/CorporateAdmin/v1/[controller]")]
  public class OnlendingLiquidationController : BaseAPIController
  {
    private readonly ILogger<OnlendingBeneficiaryController> _logger;
    private readonly IApiService _apiService;
    private readonly IOnlendingServiceApi _onlendingApi;
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;
    private readonly IConfiguration _config;
    private readonly IToken2faService _2FaService;
    private readonly INotificationService _notify;
    public OnlendingLiquidationController(
        INotificationService notify,
        ILogger<OnlendingBeneficiaryController> logger,
        IApiService apiService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor accessor,
        IEmailService emailService,
        IFileService fileService,
        IToken2faService token2FaService,
        IOnlendingServiceApi onlendingApi,
        IConfiguration config, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
    {
      _apiService = apiService;
      _emailService = emailService;
      _fileService = fileService;
      _config = config;
      _2FaService = token2FaService;
      _logger = logger;
      _notify = notify;
      _onlendingApi = onlendingApi;
    }


    [HttpGet("GetLiquidationType")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ActionResult<ResponseDTO<TransactionTypeModel>>> GetLiquidationType()
    {
      try
      {
        if (!IsAuthenticated)
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(StatusCode(401, "User is not authenticated"));
        }

        if (!IsUserActive(out string errorMsg))
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(StatusCode(400, errorMsg));
        }

        if (CorporateProfile == null)
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest("UnAuthorized Access"));
        }

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(corporateCustomerErrorMessage));
        }

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(authorizeErrorMessage));
          }
        }

        List<TransactionTypeModel> auditActions = new List<TransactionTypeModel>();
        var enums = Enum.GetValues(typeof(LiquidationType)).Cast<LiquidationType>().ToList();
        foreach (var e in enums)
        {
          auditActions.Add(new TransactionTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
        }

        return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(Ok(auditActions));

      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false)));
      }
    }

    [HttpGet("GetExtensionDate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<ActionResult<ResponseDTO<TransactionTypeModel>>> GetExtensionDate()
    {
      try
      {
        if (!IsAuthenticated)
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(StatusCode(401, "User is not authenticated"));
        }

        if (!IsUserActive(out string errorMsg))
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(StatusCode(400, errorMsg));
        }

        if (CorporateProfile == null)
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest("UnAuthorized Access"));
        }

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
        {
          return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(corporateCustomerErrorMessage));
        }

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(authorizeErrorMessage));
          }
        }

        List<TransactionTypeModel> auditActions = new List<TransactionTypeModel>();
        var enums = Enum.GetValues(typeof(LiquidationDateType)).Cast<LiquidationDateType>().ToList();
        foreach (var e in enums)
        {
          if (e.ToString() == "ThirtyDays")
          {
            auditActions.Add(new TransactionTypeModel { Key = "30", Name = "30 days" });
          }
          if (e.ToString() == "SixtyDays")
          {
            auditActions.Add(new TransactionTypeModel { Key = "60", Name = "60 days" });
          }

        }
        return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(Ok(auditActions));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false)));
      }
    }

    [HttpGet("GetLiquidationRequest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<TransactionTypeModel>>> GetLiquidationRequest(string batchId)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
        {
          return BadRequest(corporateCustomerErrorMessage);
        }

        var Id = Encryption.DecryptGuid(batchId);
        var result = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingPreliquidateBeneficiaries(Id);
				if (!result.Any())
				{
					return Ok(new { Responsecode = "00", ResponseDescription = "No Data Found", Data = result });
				}
				return Ok(new { Responsecode = "00", ResponseDescription = "Request Successful", Data = result });

			}
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetExtensionRequest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<TransactionTypeModel>>> GetExtensionRequest(string BatchId)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }
        var Id = Encryption.DecryptGuid(BatchId);
        var result = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingRepaymentExtensionRequestBeneficiaries(Id);
        if(!result.Any())
        {
					return Ok(new { Responsecode = "00", ResponseDescription = "No Data Found", Data = result });
				}
        return Ok(new { Responsecode = "00", ResponseDescription = "Request Successful", Data = result });
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return await Task.FromResult<ActionResult<ResponseDTO<TransactionTypeModel>>>(BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false)));
      }
    }

    [HttpPost("InitiateRepaymentDateExtension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> InitiateRepaymentDateExtension([FromBody] BatchRepaymentDateExtendionRequest model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }

        var payLoad = new BatchRepaymentDateExtendion
        {
          Id = Encryption.DecryptGuid(model.Id),
          Duration = Encryption.DecryptInt(model.Duration)
        };


        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
        // var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
        // if(validOTP.ResponseCode != "00"){
        //   LogFormater<BulkTransactionController>.Error(_logger,"DeclineTransaction",$"2FA API ERROR:{validOTP.ResponseMessage}",JsonConvert.SerializeObject(userName),JsonConvert.SerializeObject(corporateCustomer.CustomerId));
        //   return BadRequest(validOTP.ResponseMessage);
        // }



        //validate batch
        var paymentItem = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingCreditLogById(payLoad.Id);
        if (paymentItem is null)
        {
          return BadRequest("invalid request id!!!");
        }
        // get if batch item is still active
        if (paymentItem?.Status != (int)OnlendingStatus.Active)
        {
          return BadRequest("items on this batch has not been fully liquidated !!!");
        }

        // get beneficary facility
        var processBatch = _unitOfWork.OnlendingTransferLogRepo.GetByIdAsync((Guid)paymentItem?.TranLogId);
        if (processBatch is null)
        {
          return BadRequest("invalid Beneficiary  bebeficairy !!!");
        }

        var merchantBeneficairies = await _onlendingApi.GetMerchantBeneficairies(processBatch.DebitAccountNumber);
        if (merchantBeneficairies.ResponseCode != "00")
        {
          return BadRequest(merchantBeneficairies.ResponseDescription);
        }

        var beneficiaryLiquidation = merchantBeneficairies?.ResponseData?.FirstOrDefault(ctx => ctx.BeneficiaryAccountNumber == paymentItem?.AccountNumber && ctx.AmountDisbursted == paymentItem?.FundAmount);
        if (beneficiaryLiquidation is null)
        {
          return BadRequest("Beneficiary not found in merchant list  !!!");
        }

        var intrestResponse = new IntrestCalculationResponse();
        var calculateIntrest = new OnlendingGetInterestRequest
        {
          AccountNumber = paymentItem.AccountNumber,
          Amount = paymentItem?.FundAmount ?? 0,
          DurationIndays = payLoad?.Duration
        };
        var intrestResult = await _onlendingApi.CalculateIntrest(calculateIntrest);
        if (intrestResult.ResponseCode != "00")
        {
          return BadRequest($"Interest calculation failed, please try again: => {intrestResult.ResponseMessage}");
        }
        else
        {
          intrestResponse.AccountNumber = intrestResult?.ResponseData?.AccountNumber;
          intrestResponse.Interest = intrestResult?.ResponseData?.Interest;
        }

        var senderInfo = await _apiService.CustomerNameInquiry(processBatch.DebitAccountNumber);
        if (!AccountValidation.SourceAccount(senderInfo, out string acctErrorMessage))
        {
          return BadRequest(acctErrorMessage);
        }

        if (senderInfo.AvailableBalance < intrestResponse.Interest)
        {
          return BadRequest($"Source account cannot cover the interest payment, kindly fund the account to and continue disbursement.");
        }

        _ = Enum.TryParse(corporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);

        if (_auth == AuthorizationType.Single_Signatory)
        {
          var extendDateRequest = new OnlendingInitiateExtensionRequest
          {
            BeneficiaryId = beneficiaryLiquidation.Id,
            BeneficiaryAccountNumber = paymentItem.AccountNumber,
            MerchantOperatingAccountNumber = processBatch.OperatingAccountNumber,
            DurationIndays = payLoad.Duration
          };
          var extendDateResponse = await _onlendingApi.InitiateRepaymentDateExtension(extendDateRequest);
          if (extendDateResponse.ResponseCode != "00")
          {
            return BadRequest($"Repayment Date Extension Fail -> {extendDateResponse.ResponseDescription}");
          }
          else
          {
            paymentItem.ExtensionInterest = extendDateResponse.ResponseData.Interest;
            paymentItem.ExtensionDate = DateTime.Parse(extendDateResponse.ResponseData.ExtendedDate);
            paymentItem.Status = (int)OnlendingStatus.Extended;
          }
          return Ok(new { Responsecode = "00", ResponseDescription = "Extension request processed successfully" });
        }

        //create beneficiary 
        // create batch 
        return Ok(new { Responsecode = "00", ResponseDescription = "Extension request processed successfully" });
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("Liquidation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> Liquidation([FromBody] BatchLiquidationRequest model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }

        var payLoad = new BatchLiquidation
        {
          Id = Encryption.DecryptGuid(model.Id),
          Otp = Encryption.DecryptStrings(model.Id),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)(CorporateProfile?.CorporateCustomerId));
        var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
        // var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
        // if(validOTP.ResponseCode != "00"){
        //   LogFormater<BulkTransactionController>.Error(_logger,"DeclineTransaction",$"2FA API ERROR:{validOTP.ResponseMessage}",JsonConvert.SerializeObject(userName),JsonConvert.SerializeObject(corporateCustomer.CustomerId));
        //   return BadRequest(validOTP.ResponseMessage);
        // }

        if (corporateCustomer == null || CorporateProfile.CorporateCustomerId != corporateCustomer.Id)
        {
          return BadRequest("UnAuthorized Access");
        }

        //validate batch
        var paymentItem = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingCreditLogById(payLoad.Id);
        if (paymentItem is null)
        {
          return BadRequest("invalid request id!!!");
        }
        // get if batch item is still active
        if (paymentItem?.Status == (int)OnlendingStatus.Liquidated)
        {
          return BadRequest("items on this batch has not been fully liquidated !!!");
        }

        // get beneficary facility
        var processBatch = _unitOfWork.OnlendingTransferLogRepo.GetByIdAsync((Guid)paymentItem?.TranLogId);
        if (processBatch is null)
        {
          return BadRequest("invalid Beneficiary  bebeficairy !!!");
        }

        var merchantBeneficairies = await _onlendingApi.GetMerchantBeneficairies(processBatch.DebitAccountNumber);
        if (merchantBeneficairies.ResponseCode != "00")
        {
          return BadRequest(merchantBeneficairies.ResponseDescription);
        }

        var beneficiaryLiquidation = merchantBeneficairies?.ResponseData?.FirstOrDefault(ctx => ctx.BeneficiaryAccountNumber == paymentItem?.AccountNumber && ctx.AmountDisbursted == paymentItem?.FundAmount);
        if (beneficiaryLiquidation is null)
        {
          return BadRequest("Beneficiary not found in merchant list  !!!");
        }

        var liquadateObj = new OnlendingFullLiquidationRequest
        {
          MerchantOperatingAccountNumber = processBatch.OperatingAccountNumber,
          BeneficiaryAccountNumber = paymentItem?.AccountNumber,
          BeneficiaryId = beneficiaryLiquidation.Id
        };

        var repaymentResult = await _onlendingApi.LiquidatePayment(liquadateObj);
        if (repaymentResult.ResponseCode != "00")
        {
          return BadRequest($"Liguidation failed -> {repaymentResult.ResponseDescription}");
        }
        else
        {
          paymentItem.Status = (int)OnlendingStatus.Liquidated;
          _unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLog(paymentItem);
          _unitOfWork.Complete();
        }
        return Ok(new { Responsecode = "00", ResponseDescription = "Liguidation Successful" });

      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("PreLiquidation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> PreLiquidation([FromBody] BatchLiquidationRequest model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }

        var payLoad = new BatchLiquidation
        {
          Id = Encryption.DecryptGuid(model.Id),
          Otp = Encryption.DecryptStrings(model.Id),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)(CorporateProfile?.CorporateCustomerId));
        var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
        // var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
        // if(validOTP.ResponseCode != "00"){
        //   LogFormater<BulkTransactionController>.Error(_logger,"DeclineTransaction",$"2FA API ERROR:{validOTP.ResponseMessage}",JsonConvert.SerializeObject(userName),JsonConvert.SerializeObject(corporateCustomer.CustomerId));
        //   return BadRequest(validOTP.ResponseMessage);
        // }

        if (corporateCustomer == null || CorporateProfile.CorporateCustomerId != corporateCustomer.Id)
        {
          return BadRequest("UnAuthorized Access");
        }

        //validate batch
        var paymentItem = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingCreditLogById(payLoad.Id);
        if (paymentItem is null)
        {
          return BadRequest("invalid request id!!!");
        }
        // get if batch item is still active
        if (paymentItem?.Status == (int)OnlendingStatus.Liquidated)
        {
          return BadRequest("items on this batch has not been fully liquidated !!!");
        }

        // get beneficary facility
        var processBatch = _unitOfWork.OnlendingTransferLogRepo.GetByIdAsync((Guid)paymentItem?.TranLogId);
        if (processBatch is null)
        {
          return BadRequest("invalid Beneficiary  bebeficairy !!!");
        }

        var merchantBeneficairies = await _onlendingApi.GetMerchantBeneficairies(processBatch.DebitAccountNumber);
        if (merchantBeneficairies.ResponseCode != "00")
        {
          return BadRequest(merchantBeneficairies.ResponseDescription);
        }

        var beneficiaryLiquidation = merchantBeneficairies?.ResponseData?.FirstOrDefault(ctx => ctx.BeneficiaryAccountNumber == paymentItem?.AccountNumber && ctx.AmountDisbursted == paymentItem?.FundAmount);
        if (beneficiaryLiquidation is null)
        {
          return BadRequest("Beneficiary not found in merchant list  !!!");
        }

        var liquadateObj = new OnlendingPreLiquidationRequest
        {
          MerchantOperatingAccountNumber = processBatch.OperatingAccountNumber,
          BeneficiaryAccountNumber = paymentItem?.AccountNumber,
          BeneficiaryId = beneficiaryLiquidation.Id,
          Amount = payLoad.Amount
        };

        var repaymentResult = await _onlendingApi.PreliquidatePayment(liquadateObj);
        if (repaymentResult.ResponseCode != "00")
        {
          return BadRequest($"Pre Liguidation failed -> {repaymentResult.ResponseDescription}");
        }
        else
        {
          paymentItem.Status = (int)OnlendingStatus.PartialLiquidation;
          _unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLog(paymentItem);
          _unitOfWork.Complete();
        }
        return Ok(new { Responsecode = "00", ResponseDescription = "Pre Liguidation Successful" });

      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    // [HttpGet("UpdateBeneficiaryLiquidationStatus")]
    // [ProducesResponseType(StatusCodes.Status200OK)]
    // public async Task<ActionResult<ResponseDTO<VerifyResponse>>> UpdateBeneficiaryLiquidationStatus([FromBody] BatchLiquidationRequest model)
    // {
    //   try
    //   {
    //     if (!IsAuthenticated)
    //     {
    //       return StatusCode(401, "User is not authenticated");
    //     }

    //     if (!IsUserActive(out string errorMsg))
    //     {
    //       return StatusCode(400, errorMsg);
    //     }

    //     if (CorporateProfile == null)
    //     {
    //       return BadRequest("UnAuthorized Access");
    //     }

    //     var payLoad = new BatchLiquidation
    //     {
    //       Id = Encryption.DecryptGuid(model.Id),
    //       Otp = Encryption.DecryptStrings(model.Id),
    //       Amount = Encryption.DecryptDecimals(model.Amount),
    //       IPAddress = Encryption.DecryptStrings(model.IPAddress),
    //       HostName = Encryption.DecryptStrings(model.HostName),
    //       ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
    //       MACAddress = Encryption.DecryptStrings(model.MACAddress)
    //     };

    //     var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)(CorporateProfile?.CorporateCustomerId));
    //     var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
    //     // var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
    //     // if(validOTP.ResponseCode != "00"){
    //     //   LogFormater<BulkTransactionController>.Error(_logger,"DeclineTransaction",$"2FA API ERROR:{validOTP.ResponseMessage}",JsonConvert.SerializeObject(userName),JsonConvert.SerializeObject(corporateCustomer.CustomerId));
    //     //   return BadRequest(validOTP.ResponseMessage);
    //     // }

    //     if (corporateCustomer == null || CorporateProfile.CorporateCustomerId != corporateCustomer.Id)
    //     {
    //       return BadRequest("UnAuthorized Access");
    //     }

    //     //validate batch
    //     var paymentItem = await _unitOfWork.OnlendingCreditLogRepositoryRepo.GetOnlendingCreditLogById(payLoad.Id);
    //     if (paymentItem is null)
    //     {
    //       return BadRequest("invalid request id!!!");
    //     }
    //     // get if batch item is still active
    //     if (paymentItem?.Status != (int)OnlendingStatus.Active)
    //     {
    //       return BadRequest("items on this batch has not been fully liquidated !!!");
    //     }

    //     // get beneficary facility
    //     var processBatch = _unitOfWork.OnlendingTransferLogRepo.GetByIdAsync((Guid)paymentItem?.TranLogId);
    //     if (processBatch is null)
    //     {
    //       return BadRequest("invalid Beneficiary  bebeficairy !!!");
    //     }

    //     var merchantBeneficairies = await _onlendingApi.GetMerchantBeneficairies(processBatch.DebitAccountNumber);
    //     if (merchantBeneficairies.ResponseCode != "00")
    //     {
    //       return BadRequest(merchantBeneficairies.ResponseDescription);
    //     }

    //     var beneficiaryLiquidation = merchantBeneficairies?.ResponseData?.FirstOrDefault(ctx => ctx.BeneficiaryAccountNumber == paymentItem?.AccountNumber && ctx.AmountDisbursted == paymentItem?.FundAmount);
    //     if (beneficiaryLiquidation is null)
    //     {
    //       return BadRequest("Beneficiary not found in merchant list  !!!");
    //     }

    //     var liquadateObj = new OnlendingPreLiquidationRequest
    //     {
    //       MerchantOperatingAccountNumber = processBatch.DebitAccountNumber,
    //       BeneficiaryAccountNumber = paymentItem?.AccountNumber,
    //       BeneficiaryId = beneficiaryLiquidation.Id,
    //       Amount = payLoad.Amount
    //     };

    //     var repaymentResult = await _onlendingApi.PreliquidatePayment(liquadateObj);
    //     if (repaymentResult.ResponseCode != "00")
    //     {
    //       return BadRequest($"Liguidation failed -> {repaymentResult.ResponseDescription}");
    //     }
    //     else
    //     {
    //       paymentItem.Status = (int)OnlendingStatus.PartialLiquidation;
    //       _unitOfWork.OnlendingCreditLogRepositoryRepo.UpdateOnlendingCreditLog(paymentItem);
    //       _unitOfWork.Complete();
    //     }
    //     return Ok(true);
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
    //     return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
    //   }
    // }

    [HttpPost("ApproveRequest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> ApproveDisbursment([FromBody] InitiaOnlendingBeneficiaryDto model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        if (corporateCustomer == null || CorporateProfile.CorporateCustomerId != corporateCustomer.Id)
        {
          return BadRequest("UnAuthorized Access");
        }
        //create beneficiary 
        // create batch 
        return Ok(true);


      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("DeclineRequest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> DeclineDisbursment([FromBody] InitiaOnlendingBeneficiaryDto model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("UnAuthorized Access");
        }

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        if (corporateCustomer == null || CorporateProfile.CorporateCustomerId != corporateCustomer.Id)
        {
          return BadRequest("UnAuthorized Access");
        }
        //create beneficiary 
        // create batch 
        return Ok(true);


      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

  }
}