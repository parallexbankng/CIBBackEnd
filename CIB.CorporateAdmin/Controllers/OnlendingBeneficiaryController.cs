using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Exceptions;
using CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto;
using CIB.Core.Modules.OnLending.Beneficiary.Dto;
using CIB.Core.Modules.OnLending.TransferLog.Dto;
using CIB.Core.Services._2FA;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Services.Notification;
using CIB.Core.Services.OnlendingApi;
using CIB.Core.Services.OnlendingApi.Dto;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CIB.CorporateAdmin.Controllers
{
  [ApiController]
  [Route("api/CorporateAdmin/v1/[controller]")]
  public class OnlendingBeneficiaryController : BaseAPIController
  {
    private readonly ILogger<OnlendingBeneficiaryController> _logger;
    private readonly IApiService _apiService;
    private readonly IOnlendingServiceApi _onlendingApi;
    private readonly IEmailService _emailService;
    private readonly IFileService _fileService;
    private readonly IConfiguration _config;
    private readonly IToken2faService _2FaService;
    private readonly INotificationService _notify;
    public OnlendingBeneficiaryController(
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

    [HttpPost("BatchUpload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> VerifyBeneficiary([FromForm] VerifyOnlendingBeneficiaryDto model)
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

        var tranlg = new TblOnlendingTransferLog();
        var onlendingCreditLogList = new List<TblOnlendingCreditLog>();
        var onlendingBeneficiaryList = new List<TblOnlendingBeneficiary>();
        var accountOpeningList = new List<OnlendingBeneficiaryAccountOpeningRequest>();

        var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (!ValidationPermission.IsValidCorporateCustomer(corporateCustomer, CorporateProfile, out string corporateCustomerErrorMessage))
        {
          return BadRequest(corporateCustomerErrorMessage);
        }

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }

        var payload = new InitiaOnlendingBeneficiary
        {
          OperatingAccountNumber = Encryption.DecryptStrings(model.OperatingAccountNumber),
          SourceAccountNumber = Encryption.DecryptStrings(model.SourceAccountNumber),
          TransactionLocation = Encryption.DecryptStrings(model.TransactionLocation),
          WorkflowId = Encryption.DecryptGuid(model.WorkflowId),
          Currency = Encryption.DecryptStrings(model.Currency)
        };

        var senderInfo = await _apiService.CustomerNameInquiry(payload.SourceAccountNumber);
        if (!AccountValidation.SourceAccount(senderInfo, out string errorMessage))
        {
          return BadRequest(errorMessage);
        }

        var corporateAccount = await _apiService.RelatedCustomerAccountDetails(corporateCustomer.CustomerId);
        if (!AccountValidation.RelatedAccount(corporateAccount, payload.SourceAccountNumber, out string errMessage))
        {
          return BadRequest(errMessage);
        }

        var operationAccountInfo = await _apiService.CustomerNameInquiry(payload.OperatingAccountNumber);
        if (!AccountValidation.SourceAccount(senderInfo, out string operationAcctInfErrorMessage))
        {
          return BadRequest(operationAcctInfErrorMessage);
        }

        if (!AccountValidation.RelatedAccount(corporateAccount, payload.OperatingAccountNumber, out string operatingAcctErroMessage))
        {
          return BadRequest(operatingAcctErroMessage);
        }

        var dtb = _fileService.ReadOnlendingBeneficiariesExcelFile(model.files);
        if (dtb.Count == 0)
        {
          return BadRequest("Error Reading Excel File");
        }
        var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        if (tblCorporateCustomer == null)
        {
          return BadRequest("Invalid corporate customer id");
        }
        var batchId = Guid.NewGuid();
        var date = DateTime.Now;

        tranlg.Id = Guid.NewGuid();
        tranlg.CorporateCustomerId = tblCorporateCustomer.Id;
        tranlg.InitiatorId = CorporateProfile.Id;
        tranlg.InitiatorUserName = CorporateProfile.Username;
        tranlg.DebitAccountName = senderInfo.AccountName;
        tranlg.DebitAccountNumber = payload.SourceAccountNumber;
        tranlg.OperatingAccountNumber = payload.OperatingAccountNumber;
        tranlg.OperatingAccountName = operationAccountInfo.AccountName;
        tranlg.PostingType = "Onlending";
        tranlg.Currency = payload.Currency;
        tranlg.TransactionStatus = 0;
        tranlg.TransferType = nameof(TransactionType.OnLending);
        tranlg.BatchId = batchId;
        tranlg.PostingType = "Onlending";
        tranlg.Currency = payload.Currency;
        tranlg.TransactionStatus = 0;
        tranlg.TransferType = nameof(TransactionType.OnLending);
        tranlg.BatchId = batchId;
        tranlg.TransactionStatus = 0;
        tranlg.ApprovalStatus = 0;
        tranlg.ApprovalStage = 1;
        tranlg.TransactionLocation = payload.TransactionLocation;
        tranlg.Sn = 0;
        tranlg.TotalCredit = 0;
        tranlg.TotalFailed = 0;
        tranlg.Status = 0;
        tranlg.DateInitiated = DateTime.Now;
        var bulkTransactionItems = new List<BeneficiaryDto>();
        var response = new VerifyResponse();

        await Task.WhenAll(dtb.AsEnumerable().Select(async row =>
        {
          string errorMsg = "";
          var beneficiaryCreditLog = new TblOnlendingCreditLog();

          if (string.IsNullOrWhiteSpace(row.SurName))
          {
            beneficiaryCreditLog.VerificationStatus = 2;
            errorMsg = "Surname is required";
          }


          if (string.IsNullOrWhiteSpace(row.FirstName))
          {
            errorMsg += "|First Name is required";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (string.IsNullOrWhiteSpace(row.PhoneNo))
          {
            errorMsg += "|PhoneNo is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (string.IsNullOrWhiteSpace(row.Address))
          {
            errorMsg += "|Address is required; ";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (string.IsNullOrWhiteSpace(row.StreetNo))
          {
            errorMsg += "|StreetNo is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }


          if (string.IsNullOrWhiteSpace(row.State))
          {
            errorMsg += "|State is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (string.IsNullOrWhiteSpace(row.City))
          {
            errorMsg += "|City is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (string.IsNullOrWhiteSpace(row.Lga))
          {
            errorMsg += "|Lga is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }


          if (string.IsNullOrWhiteSpace(row.StreetNo))
          {
            errorMsg += "|Address is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (string.IsNullOrWhiteSpace(row.PreferredNarration))
          {
            errorMsg += "|PreferredNarration is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }


          if (string.IsNullOrWhiteSpace(row.DateIssued))
          {
            errorMsg += "|Date Issued is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }


          if (string.IsNullOrWhiteSpace(row.DateOfBirth))
          {
            errorMsg += "|Date Of Birth is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }


          if (string.IsNullOrWhiteSpace(row.RepaymentDate))
          {
            errorMsg += "|Repayment Date is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }


          if (string.IsNullOrWhiteSpace(row?.DocType))
          {
            errorMsg += "|Doc Type is required;";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (row?.Bvn != null && row?.Bvn.Length != 11)
          {
            errorMsg += "|Invalid BVN Number";
            beneficiaryCreditLog.VerificationStatus = 2;
          }

          if (bulkTransactionItems.Any())
          {
            var duplicateBvn = bulkTransactionItems?.Any(xtc => xtc.Bvn == row?.Bvn);
            var duplicatePhone = bulkTransactionItems?.Any(xtc => xtc.PhoneNo == row?.PhoneNo);
            var duplicateEmail = bulkTransactionItems?.Any(xtc => xtc.Email == row?.Email);

            if ((bool)duplicateBvn)
            {
              errorMsg += $"|Bvn {row?.Bvn} Already Exists";
              beneficiaryCreditLog.VerificationStatus = 2;
            }


            if ((bool)duplicatePhone)
            {
              errorMsg += $"|Phone {row?.PhoneNo} Already Exists";
              beneficiaryCreditLog.VerificationStatus = 2;
            }


            if ((bool)duplicateEmail)
            {
              errorMsg += $"|Email {row?.Email} Already Exists";
              beneficiaryCreditLog.VerificationStatus = 2;
            }

          }

          if (string.IsNullOrWhiteSpace(errorMsg.ToString()) || errorMsg.ToString().Contains($"this BVN {row.Bvn} Already Exists") || errorMsg.ToString().Contains($"this BVN {row.Bvn} Already Exists"))
          {
            var narration2 = row?.PreferredNarration != null && row?.PreferredNarration.Length > 50 ? row.PreferredNarration[..50] : row?.PreferredNarration;
            var dateissue = DateTime.Parse(row?.DateIssued);
            var repaymentDate = DateTime.Parse(row?.RepaymentDate);
            var dob = DateTime.Parse(row?.DateOfBirth);
            var validateBVN = await _onlendingApi.TestValidateBvn(row.Bvn);
            if (validateBVN?.ResponseCode != "00")
            {
              errorMsg += "|Bvn is invalid";
              beneficiaryCreditLog.VerificationStatus = 2;

              var beneficiary = new TblOnlendingBeneficiary
              {
                Id = Guid.NewGuid(),
                CorporateCustomerId = tblCorporateCustomer.Id,
                Sn = 0,
                Title = row.Title,
                SurName = row.SurName,
                FirstName = row.FirstName,
                MiddleName = row.MiddleName,
                PhoneNo = row.PhoneNo,
                Email = row.Email,
                Gender = row.Gender,
                Address = row.Address,
                DateOfBirth = dob,
                Bvn = row.Bvn,
                DocType = row.DocType,
                IdNumber = row.IdNumber,
                IdIssuedDate = dateissue,
                DateCreated = DateTime.Now,
                StreetNo = row.StreetNo,
                City = row.City,
                State = row.State,
                Lga = row.Lga,
                StateOfResidence = row.StateOfResidence,
                PlaceOfBirth = row.PlaceOfBirth,
                MaritalStatus = row.MaritalStatus,
                Region = row.Region,
              };

              beneficiaryCreditLog.Id = Guid.NewGuid();
              beneficiaryCreditLog.Sn = 0;
              beneficiaryCreditLog.BeneficiaryId = beneficiary.Id;
              beneficiaryCreditLog.CorporateCustomerId = tblCorporateCustomer.Id;
              beneficiaryCreditLog.FundAmount = row.FundAmount;
              beneficiaryCreditLog.RepaymentDate = repaymentDate;
              beneficiaryCreditLog.BatchId = tranlg.BatchId;
              beneficiaryCreditLog.TranLogId = tranlg.Id;
              beneficiaryCreditLog.DateInitiated = DateTime.UtcNow;
              beneficiaryCreditLog.Narration = narration2;
              beneficiaryCreditLog.DateCreated = DateTime.UtcNow;
              beneficiaryCreditLog.Error = errorMsg;
              beneficiaryCreditLog.BvnResponse = validateBVN?.ResponseCode;
              beneficiaryCreditLog.BvnResponseCode = validateBVN?.ResponseCode;
              onlendingBeneficiaryList.Add(beneficiary);
              onlendingCreditLogList.Add(beneficiaryCreditLog);
            }
            else
            {
              if (!string.Equals(validateBVN?.FirstName.Trim(), row?.FirstName.Trim(), StringComparison.OrdinalIgnoreCase))
              {
                errorMsg += "|Bvn First Name doesnot Match";
                beneficiaryCreditLog.VerificationStatus = 2;
              }


              if (!string.Equals(validateBVN.LastName.Trim(), row.SurName.Trim(), StringComparison.OrdinalIgnoreCase))
              {
                errorMsg += "|Bvn Last Name doesnot Match";
                beneficiaryCreditLog.VerificationStatus = 2;
              }

              if (!string.Equals(validateBVN.DateOfBirth.Trim(), DateTime.Parse(row.DateOfBirth).ToString("dd-MMM-yyyy")))
              {
                errorMsg += "|Invalid Date of birth";
                beneficiaryCreditLog.VerificationStatus = 2;
              }

              var checkForDoubleRequest = UnitOfWork.OnlendingCreditLogRepositoryRepo.CheckForDoubleOnlendingRequestByBVN(row.Bvn);
              if (checkForDoubleRequest)
              {
                errorMsg += "|beneficiary Already Has an onlending facility with another merchant;";
                beneficiaryCreditLog.VerificationStatus = 2;
              }

              var beneficiary = new TblOnlendingBeneficiary
              {
                Id = Guid.NewGuid(),
                CorporateCustomerId = tblCorporateCustomer.Id,
                Sn = 0,
                Title = row.Title,
                SurName = row.SurName,
                FirstName = row.FirstName,
                MiddleName = row.MiddleName,
                PhoneNo = row.PhoneNo,
                Email = row.Email,
                Gender = row.Gender,
                Address = row.Address,
                DateOfBirth = dob,
                Bvn = row.Bvn,
                DocType = row.DocType,
                IdNumber = row.IdNumber,
                IdIssuedDate = dateissue,
                DateCreated = DateTime.Now,
                StreetNo = row.StreetNo,
                City = row.City,
                State = row.State,
                Lga = row.Lga,
                StateOfResidence = row.StateOfResidence,
                PlaceOfBirth = row.PlaceOfBirth,
                MaritalStatus = row.MaritalStatus,
                Region = row.Region,
              };

              beneficiaryCreditLog.Id = Guid.NewGuid();
              beneficiaryCreditLog.Sn = 0;
              beneficiaryCreditLog.BeneficiaryId = beneficiary.Id;
              beneficiaryCreditLog.CorporateCustomerId = tblCorporateCustomer.Id;
              beneficiaryCreditLog.FundAmount = row.FundAmount;
              beneficiaryCreditLog.RepaymentDate = repaymentDate;
              beneficiaryCreditLog.BatchId = tranlg.BatchId;
              beneficiaryCreditLog.TranLogId = tranlg.Id;
              beneficiaryCreditLog.DateInitiated = DateTime.UtcNow;
              beneficiaryCreditLog.Narration = narration2;
              beneficiaryCreditLog.DateCreated = DateTime.UtcNow;
              beneficiaryCreditLog.Error = errorMsg.ToString();
              beneficiaryCreditLog.BvnResponse = validateBVN.ResponseMessage;
              beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode;
              beneficiaryCreditLog.VerificationStatus = beneficiaryCreditLog.VerificationStatus != null ? beneficiaryCreditLog.VerificationStatus : 1;
              onlendingBeneficiaryList.Add(beneficiary);
              onlendingCreditLogList.Add(beneficiaryCreditLog);
            }
          }

        }));

        decimal? totalAmount = dtb.Sum(ctx => ctx.FundAmount);
        var processAmount = onlendingCreditLogList.Sum(xr => xr.FundAmount);
        var validAmount = onlendingCreditLogList.Where(ctz => ctz.VerificationStatus == 1).Sum(xr => xr.FundAmount);

        if (totalAmount != processAmount)
        {
          return BadRequest("Total Amount is not valid");
        }

        _ = Enum.TryParse(corporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);
        tranlg.TotalValidAmount = validAmount;
        tranlg.NumberOfCredit = onlendingCreditLogList.Count;
        tranlg.ValidCount = onlendingCreditLogList.Count(ctx => ctx.VerificationStatus == 1);
        tranlg.InValidCount = onlendingCreditLogList.Count(ctx => ctx.VerificationStatus == 2);
        tranlg.TotalAmount = processAmount;
        tranlg.DateInitiated = DateTime.Now;


        if (_auth == AuthorizationType.Single_Signatory)
        {
          tranlg.ApprovalStatus = 0;
          tranlg.ApprovalStage = 1;
          _unitOfWork.OnlendingBeneficiaryRepo.AddRange(onlendingBeneficiaryList);
          _unitOfWork.OnlendingTransferLogRepo.Add(tranlg);
          _unitOfWork.OnlendingCreditLogRepositoryRepo.AddRange(onlendingCreditLogList);
          _unitOfWork.Complete();
          return Ok(new ResponseDTO<VerifyResponse>(_data: response, success: true, _message: Message.Success));
        }
        else
        {
          var approvelList = new List<TblCorporateApprovalHistory>();
          var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId((Guid)payload?.WorkflowId);
          if (!workflowHierarchies.Any())
          {
            return BadRequest("Authorizer has not been set");
          }

          var workflow = _unitOfWork.WorkFlowRepo.GetByIdAsync((Guid)payload?.WorkflowId);
          if (workflow is null)
          {
            return BadRequest("Authorizer has not been set");
          }

          if (!WorkflowValidation.Validate(workflow, workflowHierarchies, totalAmount, out string errMsg))
          {
            return BadRequest(errMsg);
          }

          var auditTrail = new TblAuditTrail
          {
            Id = Guid.NewGuid(),
            ActionCarriedOut = nameof(AuditTrailAction.Bulk_Bank_Transfer).Replace("_", " "),
            Ipaddress = payload.IPAddress,
            Macaddress = payload.MACAddress,
            HostName = payload.HostName,
            ClientStaffIpaddress = payload.ClientStaffIPAddress,
            NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated Onlending Transaction of " + totalAmount + "from " + payload.SourceAccountNumber,
            PreviousFieldValue = "",
            TransactionId = "",
            UserId = CorporateProfile.Id,
            Username = CorporateProfile.Username,
            Description = "Corporate User Initiated Onlending Transaction",
            TimeStamp = DateTime.Now
          };

          foreach (var item in workflowHierarchies)
          {
            var toApproved = item.AuthorizationLevel == 1 ? 1 : 0;
            var corporateApprovalHistory = new TblCorporateApprovalHistory
            {
              Id = Guid.NewGuid(),
              LogId = tranlg.Id,
              Status = nameof(AuthorizationStatus.Pending),
              ApprovalLevel = item?.AuthorizationLevel,
              ApproverName = item?.ApproverName,
              Description = $"Authorizer {item?.AuthorizationLevel}",
              Comment = "",
              UserId = item?.ApproverId,
              ToApproved = toApproved,
              CorporateCustomerId = tblCorporateCustomer.Id
            };
            approvelList.Add(corporateApprovalHistory);
          }

          tranlg.ApprovalStatus = 0;
          tranlg.ApprovalStage = 1;
          tranlg.ApprovalCount = workflowHierarchies.Count;
          tranlg.WorkflowId = payload.WorkflowId;
          _unitOfWork.OnlendingBeneficiaryRepo.AddRange(onlendingBeneficiaryList);
          _unitOfWork.OnlendingTransferLogRepo.Add(tranlg);
          _unitOfWork.OnlendingCreditLogRepositoryRepo.AddRange(onlendingCreditLogList);
          _unitOfWork.CorporateApprovalHistoryRepo.AddRange(approvelList);
          _unitOfWork.Complete();
          var firstApproval = approvelList.First(ctx => ctx.ApprovalLevel == 1);
          var corporateUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)firstApproval?.UserId);
          var initiatorName = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)tranlg.InitiatorId);
          ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(corporateUser.Email, initiatorName.FullName, string.Format("{0:0.00}", tranlg.TotalAmount))));
        }
        return Ok(new { Responsecode = "00", ResponseDescription = "Onlending Request has been forwarded for approval" });
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetBatches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> GetBatches()
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }

        var batchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetAllOnlendingBatches((Guid)CorporateProfile?.CorporateCustomerId);
        return Ok(new ResponseDTO<List<TblOnlendingTransferLog>>(_data: batchInfo.ToList(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("DownloadInvalidBatch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> DownloadInvalidBatch(string batchId)
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }

        var Id = Encryption.DecryptGuid(batchId);
        var batchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingInvalidBatchByBatchId(Id);
        return Ok(new ResponseDTO<List<BeneficiaryDto>>(_data: batchInfo.ToList(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetProcessBatches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> GetValidBatch()
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }

        var batchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingCorporateValidBatch(corporateCustomer.Id);
        return Ok(new ResponseDTO<List<ReportResponse>>(_data: batchInfo.ToList(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetProcessBatcheBeneficiaries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> GetBatchBeneficiaries(string batchId)
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }


        var id = Encryption.DecryptGuid(batchId);
        var batchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingCorporateValidBatchBeneficiary(id);
        return Ok(new ResponseDTO<List<ReportListResponse>>(_data: batchInfo.ToList(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("DownloadBatch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> DownloadAllBatch(string batchId)
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }

        var Id = Encryption.DecryptGuid(batchId);
        var batchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingBatchBeneficiaryBatchId(Id);
        return Ok(new ResponseDTO<List<BeneficiaryDto>>(_data: batchInfo.ToList(), success: true, _message: Message.Success));


      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("InitiateValidBatch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> InitiateValidBatch([FromBody] OnLendingInitiateBatchRequest model)
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }
        // get by batch
        var payload = new InitiateDisbursment
        {
          BatchId = Encryption.DecryptGuid(model.BatchId),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };


        var batchInfos = await _unitOfWork.OnlendingTransferLogRepo.GetOnlendingValidBatchByBatchId(payload.BatchId);
        if (!batchInfos.Any())
        {
          return BadRequest("No valid batch was found!!");
        }
        var additionInfo = await _onlendingApi.TestGetBeneficiaryAddressInfo();
        foreach (var item in batchInfos)
        {

          // "BVN": "00000988888",
          // "Title": "MR",
          // "FirstName": "Olufemi",
          // "MiddleName": "",
          // "LastName": "Ojo",
          // "PhoneNumber": "2347038611302",
          // "EmailAddress": null,
          // "MaritalStatus": "MARR",
          // "Gender": "M",
          // "streetNo": "13",
          // "Address": "Lagos, Nigeria",
          // "City": "0534",
          // "State": "15",
          // "LGA": "01256",
          // "Region": "Lagos",
          // "DateOfBirth": "1995-09-27",
          // "PlaceOfBirth": "Lagos",
          // "CountryOfResidence": "NG",
          // "EmploymentStatus": "Employed",
          // "Occupation": "DOCTR",
          // "Nationality": "Nigerian",
          // "ReferralCode": null,
          // "ChannelCode": "",
          // "SchmCode": "GBCAI”,GLBSA
          // "AccountType": "CURRENT",
          // "StateOfResidence": "Lagos",
          // "RequestID": "323535646776474740"



          var itemInfo = GetBeneficiaryStateAndCityCode(additionInfo, item?.State, item?.City);
          var accountOpen = new OnlendingBeneficiaryAccountOpeningRequest
          {
            BVN = item.Bvn,
            Title = item.Title,
            FirstName = item.FirstName,
            MiddleName = item.MiddleName,
            LastName = item.SurName,
            PhoneNumber = item.PhoneNo,
            EmailAddress = item.Email,
            MaritalStatus = "MARR",
            Gender = item?.Gender?.Trim().ToLower() == "female" ? "F" : "M",
            streetNo = item?.StreetNo,
            Address = item?.Address,
            City = "0534",
            State = itemInfo.StateCode,
            LGA = itemInfo.LgaCode,
            Region = item?.Region,
            DateOfBirth = item?.DateOfBirth,
            Nationality = "Nigerian",
            StateOfResidence = item?.StateOfResidence,
            RequestID = Generate16DigitNumber.Create16DigitString(),
            ReferralCode = null,
            PlaceOfBirth = item?.PlaceOfBirth,
            CountryOfResidence = "NG",
            EmploymentStatus = "OTHER",
            Occupation = "DOCTR",
            ChannelCode = "2",
            SchmCode = "GLBSA",
            AccountType = "SAVINGS"
          };
          var createBeneficiaryAccount = await _onlendingApi.AccountOpening(accountOpen);
          if (createBeneficiaryAccount.ResponseCode != "00")
          {

          }
          else
          {
            var getBeneficiaryAccount = _unitOfWork.OnlendingBeneficiaryRepo.GetByIdAsync((Guid)item.Id);
            getBeneficiaryAccount.AccountNumber = createBeneficiaryAccount?.ResponseData?.AccountNumber;
            _unitOfWork.OnlendingBeneficiaryRepo.UpdateOnlendingBeneficiary(getBeneficiaryAccount);
            _unitOfWork.Complete();
          }

        }
        return Ok(true);
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetValidBatcheBeneficiaries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> GetValidBatcheBeneficiaries(string batchId)
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

        if (!UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.InitiateTransaction))
        {
          if (!ValidationPermission.IsAuthorized(corporateCustomer, out string authorizeErrorMessage))
          {
            return BadRequest(authorizeErrorMessage);
          }
        }

        var id = Encryption.DecryptGuid(batchId);
        var batchInfo = await _unitOfWork.OnlendingTransferLogRepo.GetValidBatchBeneficiariesByBatchId(id);
        return Ok(new ResponseDTO<List<BatchBeneficaryResponse>>(_data: batchInfo.ToList(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("DownloadTemplate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterbankUploadSample()
    {
      try
      {
        if (!IsAuthenticated)
        {
          return StatusCode(401, "User is not authenticated");
        }
        //path to file
        var filePath = Path.Combine("wwwroot", "bulkupload", "onlendingtemplate.xlsx");
        if (!System.IO.File.Exists(filePath))
          return NotFound();
        var memory = new MemoryStream();
        await using (var stream = new FileStream(filePath, FileMode.Open))
        {
          await stream.CopyToAsync(memory);
        }
        memory.Position = 0;
        return File(memory, Formater.GetContentType(filePath), "bulk upload onlendingtemplate.xlsx");
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    [HttpPost("VerifyBvn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> VerifyBvn(string bvn)
    {
      try
      {
        var result = await _onlendingApi.TestValidateBvn(bvn);
        return Ok(result);
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("VerifyItem")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseDTO<VerifyResponse>>> VerifyItem(string stateName, string city)
    {
      try
      {
        var result = await _onlendingApi.TestGetBeneficiaryAddressInfo();
        string[] words = stateName.Split(" ");
        var state = words.Length > 2 ? $"{words[0]} {words[1]}" : words.Length > 1 ? $"{words[0]}" : $"{words[0]}";
        var beneficiaryState = result?.State?.Where(ctx => ctx.StateName.Trim().ToLower() == state.ToLower()).FirstOrDefault();
        var beneficiaryCity = beneficiaryState?.Lga.Where(ctx => ctx.Lga.Trim().ToLower() == city.ToLower()).FirstOrDefault();
        return Ok(beneficiaryCity);
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    private static BeneficiaryStateResponse GetBeneficiaryStateAndCityCode(BeneficiaryAdditionInfoRespons result, string stateName, string city)
    {
      string[] words = stateName.Split(" ");
      var state = words.Length > 2 ? $"{words[0]} {words[1]}" : words.Length > 1 ? $"{words[0]}" : $"{words[0]}";
      var beneficiaryState = result?.State?.Where(ctx => ctx.StateName.Trim().ToLower() == state.ToLower()).FirstOrDefault();
      var beneficiaryCity = beneficiaryState?.Lga.Where(ctx => ctx.Lga.Trim().ToLower() == city.ToLower()).FirstOrDefault(); var response = new BeneficiaryStateResponse
      {
        State = beneficiaryState?.StateName,
        StateCode = beneficiaryState?.StateCode,
        Lga = beneficiaryCity?.Lga,
        LgaCode = beneficiaryCity?.LgaCode

      };
      return response;
    }


    // public async  List<BeneficiaryDto> ValidateBeneficiary(List<BeneficiaryDto> dtb)
    // {
    //     await Task.WhenAll(dtb.AsEnumerable().Select(async row => {
    //         var errorMsg = "";
    //         if (string.IsNullOrEmpty(row.SurName) || string.IsNullOrEmpty(row.SurName?.Trim()))
    //         {
    //             errorMsg = "Surname  is require;";
    //         }
    //         if (string.IsNullOrEmpty(row.FirstName) || string.IsNullOrEmpty(row.FirstName?.Trim()))
    //         {
    //             errorMsg += "FirstName  is require";
    //         }

    //         if (string.IsNullOrEmpty(row.PhoneNo) || string.IsNullOrEmpty(row.PhoneNo?.Trim()))
    //         {
    //             errorMsg += "PhoneNo  is require";
    //         }
    //         if (string.IsNullOrEmpty(row.Address) || string.IsNullOrEmpty(row.Address?.Trim()))
    //         {
    //             errorMsg += "Address  is require";
    //         }
    //         if (string.IsNullOrEmpty(row.Gender) || string.IsNullOrEmpty(row.Gender?.Trim()))
    //         {
    //             errorMsg += "Gender  is require";
    //         }
    //         if (string.IsNullOrEmpty(row.PreferredNarration) || string.IsNullOrEmpty(row.PreferredNarration?.Trim()))
    //         {
    //             errorMsg += "Preferred Narration  is require";
    //         }
    //         if (string.IsNullOrEmpty(row.DateIssued) || string.IsNullOrEmpty(row.DateIssued))
    //         {
    //             errorMsg += "Id Date Issued  is require";
    //         }
    //         if (string.IsNullOrEmpty(row.DateOfBirth) || string.IsNullOrEmpty(row.DateOfBirth))
    //         {
    //             errorMsg += "Date Of Birth  is require";
    //         }

    //         if (string.IsNullOrEmpty(row.RepaymentDate) || string.IsNullOrEmpty(row.RepaymentDate))
    //         {
    //             errorMsg += "Repayment Date  is require";
    //         }

    //         if(bulkTransactionItems.Any())
    //         {  
    //             var duplicateBvn = bulkTransactionItems?.Where(xtc => xtc.Bvn == row.Bvn && xtc.Bvn == row.Bvn).ToList();
    //             var duplicatePhone = bulkTransactionItems?.Where(xtc => xtc.PhoneNo == row.PhoneNo && xtc.PhoneNo == row.PhoneNo).ToList();
    //             var duplicateEmail = bulkTransactionItems?.Where(xtc => xtc.Email == row.Email && xtc.Email == row.Email).ToList();
    //             if(duplicateBvn.Any())
    //             {
    //                 errorMsg += $"Bvn {row.Bvn} Already Exist";
    //             }
    //             if(duplicatePhone.Any())
    //             {
    //                 errorMsg += $"Phone {row.PhoneNo} Already Exist";
    //             }
    //             if(duplicateEmail.Any())
    //             {
    //                 errorMsg += $"Email {row.Email} Already Exist";
    //             }
    //         }

    //         if (string.IsNullOrEmpty(errorMsg))
    //         {
    //             var beneficairyStateAndLga = GetBeneficiaryStateAndCityCode(additionInfo,row.State,row.City);
    //             var narration2 = row.PreferredNarration.Length > 50 ? row.PreferredNarration[..50] : row.PreferredNarration;
    //             var dateissue = DateTime.Parse(row.DateIssued);
    //             var repaymentDate = DateTime.Parse(row.RepaymentDate);
    //             var dob = DateTime.Parse(row.DateOfBirth);
    //             var beneficiary = new TblOnlendingBeneficiary
    //             {
    //                 Id = Guid.NewGuid(),
    //                 CorporateCustomerId = tblCorporateCustomer.Id,
    //                 Sn = 0,
    //                 SurName = row.SurName,
    //                 FirstName = row.FirstName,
    //                 MiddleName = row.MiddleName,
    //                 PhoneNo = row.PhoneNo,
    //                 Email = row.Email,
    //                 Gender = row.Gender,
    //                 Address = row.Address,
    //                 DateOfBirth = dob ,
    //                 Bvn = row.Bvn,
    //                 DocType = row.DocType,
    //                 IdNumber = row.IdNumber,
    //                 IdIssuedDate = dateissue,
    //                 DateCreated=DateTime.Now,
    //                 StreetNo = row.StreetNo,
    //                 City = row.City,
    //                 State = row.State,
    //                 Lga = row.Lga,
    //                 StateOfResidence = row.StateOfResidence,
    //                 PlaceOfBirth = row.PlaceOfBirth,
    //                 MaritalStatus = row.MaritalStatus,
    //                 Region = row.Region,
    //             }; 
    //             var beneficiaryCreditLog = new TblOnlendingCreditLog
    //             {
    //                 Id = Guid.NewGuid(),
    //                 Sn = 0,
    //                 BeneficiaryId = beneficiary.Id,
    //                 CorporateCustomerId = tblCorporateCustomer.Id,
    //                 FundAmount = row.FundAmount,
    //                 RepaymentDate = repaymentDate,
    //                 BatchId =  tranlg.BatchId,
    //                 TranLogId = tranlg.Id,
    //                 DateInitiated = DateTime.UtcNow,
    //                 Narration = narration2
    //             };
    //             var validateBVN = await _onlendingApi.TestValidateBvn(row.Bvn);
    //             if(validateBVN.ResponseCode != "00")
    //             {
    //                 beneficiaryCreditLog.BvnResponse = validateBVN.ResponseMessage;
    //                 beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode;
    //                 beneficiaryCreditLog.VerificationStatus = 2;
    //             }
    //             else
    //             {
    //                 if (!string.Equals(validateBVN.FirstName.Trim().ToLower(), row.FirstName.Trim().ToLower(), StringComparison.OrdinalIgnoreCase))
    //                 {
    //                     beneficiaryCreditLog.BvnResponse = "FirstName Is Not Match";
    //                     beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode; 
    //                     beneficiaryCreditLog.VerificationStatus = 2;
    //                 }
    //                 else
    //                 {
    //                     beneficiaryCreditLog.BvnResponse = validateBVN.ResponseMessage;
    //                     beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode;
    //                     beneficiaryCreditLog.VerificationStatus = 1;
    //                 }

    //                 if (!string.Equals(validateBVN.LastName.Trim().ToLower(), row.SurName.Trim().ToLower(), StringComparison.OrdinalIgnoreCase))
    //                 {
    //                     beneficiaryCreditLog.BvnResponse = "SurName Is Not Match";
    //                     beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode; 
    //                     beneficiaryCreditLog.VerificationStatus = 2;
    //                 }
    //                 else
    //                 {
    //                     beneficiaryCreditLog.BvnResponse = validateBVN.ResponseMessage;
    //                     beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode;
    //                     beneficiaryCreditLog.VerificationStatus = 1;
    //                 }

    //                 if (!string.Equals(validateBVN.DateOfBirth.Trim(), DateTime.Parse(row.DateOfBirth).ToString("dd-MMM-yyyy")))
    //                 {
    //                     beneficiaryCreditLog.BvnResponse = "Date of birth Is Not Match";
    //                     beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode; 
    //                     beneficiaryCreditLog.VerificationStatus = 2;
    //                 }
    //                 else
    //                 {
    //                     beneficiaryCreditLog.BvnResponse = validateBVN.ResponseMessage;
    //                     beneficiaryCreditLog.BvnResponseCode = validateBVN.ResponseCode;
    //                     beneficiaryCreditLog.VerificationStatus = 1;
    //                 }
    //             }

    //             if(beneficiaryCreditLog.VerificationStatus == 1)
    //             {
    //                 var checkForDoubleRequest = UnitOfWork.OnlendingCreditLogRepositoryRepo.CheckForDoubleOnlendingRequestByBVN(row.Bvn);
    //                 if(checkForDoubleRequest)
    //                 {
    //                     beneficiaryCreditLog.ResponseCode = ResponseCode.DUPLICATE_VALUE;
    //                     beneficiaryCreditLog.ResponseMessage = "beneficiary Already Have and onlending facility with another matchant"; 
    //                     beneficiaryCreditLog.VerificationStatus = 2;
    //                 }
    //                 else
    //                 {
    //                     var accountOpen = new OnlendingBeneficiaryAccountOpeningRequest
    //                     {
    //                         BVN= row.Bvn,
    //                         Title= validateBVN.Title,
    //                         FirstName=validateBVN.FirstName,
    //                         MiddleName=validateBVN.MiddleName,
    //                         LastName= validateBVN.LastName,
    //                         PhoneNumber= validateBVN.PhoneNumber1,
    //                         EmailAddress= row.Email,
    //                         MaritalStatus= validateBVN.MaritalStatus,
    //                         Gender= validateBVN.Gender,
    //                         streetNo= validateBVN.ResidentialAddress,
    //                         Address= validateBVN.ResidentialAddress,
    //                         City= beneficairyStateAndLga.LgaCode,
    //                         State= beneficairyStateAndLga.StateCode,
    //                         LGA= beneficairyStateAndLga.LgaCode,
    //                         Region= row.Region,
    //                         DateOfBirth= validateBVN.DateOfBirth,
    //                         Nationality=validateBVN.Nationality,
    //                         StateOfResidence = validateBVN.StateOfResidence,
    //                         RequestID=Generate16DigitNumber.Create16DigitString(),
    //                         ReferralCode= null,
    //                         PlaceOfBirth=row.PlaceOfBirth,
    //                         CountryOfResidence="NG",
    //                         EmploymentStatus="OTHERS",
    //                         Occupation="OTHERS",
    //                         ChannelCode="3",
    //                         SchmCode="GLBSA",
    //                         AccountType="SAVINGS"
    //                     };
    //                     onlendingBeneficiaryList.Add(beneficiary);
    //                     onlendingCreditLogList.Add(beneficiaryCreditLog);
    //                     accountOpeningList.Add(accountOpen);
    //                 }
    //             }
    //             else
    //             {

    //             }
    //         }
    //     }));

    // }

  }
}