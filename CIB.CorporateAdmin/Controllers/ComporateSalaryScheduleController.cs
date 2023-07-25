
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateCustomerSalary.Validation;
using CIB.Core.Modules.CorporateProfile.Dto;
using CIB.Core.Modules.CorporateSalarySchedule.Dto;
using CIB.Core.Modules.Transaction.Dto;
using CIB.Core.Modules.Transaction.Dto.Intrabank;
using CIB.Core.Services.Api;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.Notification;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CIB.CorporateAdmin.Controllers
{
  [ApiController]
  [Route("api/CorporateAdmin/v1/[controller]")]
  public class ComporateSalaryScheduleController : BaseAPIController
  {
    private readonly ILogger _logger;
    private readonly IApiService _apiService;
    private readonly IConfiguration _config;
    private readonly INotificationService _notify;
    private readonly IEmailService _emailService;

    public ComporateSalaryScheduleController(IEmailService emailService, INotificationService notify, IConfiguration config, IApiService apiService, ILogger<ComporateSalaryScheduleController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
    {
      _logger = logger;
      _apiService = apiService;
      _config = config;
      _notify = notify;
      _emailService = emailService;
    }

    [HttpPost("CreateSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblTempCorporateSalarySchedule>> CreateSalarySchedule(CreateCorporateCustomerSalaryDtoRequest model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateSchedule))
        {
          if (_auth != AuthorizationType.Single_Signatory)
          {
            return BadRequest("UnAuthorized Access");
          }

        }

        var payload = new CreateCorporateCustomerSalaryDto
        {
          CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
          AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
          Frequency = Encryption.DecryptStrings(model.Frequency),
          TriggerType = Encryption.DecryptStrings(model.TriggerType),
          NumberOfBeneficairy = Encryption.DecryptStrings(model.NumberOfBeneficairy),
          StartDate = Encryption.DecryptDateTime(model.StartDate),
          Discription = Encryption.DecryptStrings(model.Discription),
          IsSalary = Encryption.DecryptBooleans(model.IsSalary),
          WorkFlowId = Encryption.DecryptGuid(model.WorkFlowId),
          TransactionLocation = Encryption.DecryptStrings(model.TransactionLocation),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };

        if (ValidatePayload(out errormsg, payload, null))
        {
          return BadRequest(errormsg);
        }

        var mapSchedule = this.MapCreateRequestDtoToCorporateCustomerSalary(payload);
        var check = UnitOfWork.CorporateSalaryScheduleRepo.CheckDuplicate(mapSchedule);
        if (check.IsDuplicate)
        {
          return BadRequest(check.Message);
        }

        var mapTempSchedule = this.MapCreateRequestDtoToTempCorporateCustomerSalary(payload);
        var checkTemp = UnitOfWork.TempCorporateSalaryScheduleRepo.CheckDuplicate(mapTempSchedule);
        if (checkTemp.IsDuplicate)
        {
          return BadRequest(check.Message);
        }

        if (_auth != AuthorizationType.Single_Signatory)
        {
          var isValidId = Guid.TryParse(payload.WorkFlowId.ToString(), out _);
          if (!isValidId)
          {
            return BadRequest("Workflow Id is require");
          }
          mapTempSchedule.Action = nameof(TempTableAction.Create).Replace("_", " ");
          mapTempSchedule.IsTreated = (int)ProfileStatus.Pending;
          mapTempSchedule.Status = (int)ProfileStatus.Pending;
          UnitOfWork.TempCorporateSalaryScheduleRepo.Add(mapTempSchedule);
        }
        else
        {
          mapSchedule.Status = 1;
          UnitOfWork.CorporateSalaryScheduleRepo.Add(mapSchedule);
        }


        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Create).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {payload.AccountNumber}, " +
            $"Frequency: {payload.Frequency}, TriggerType: {payload.TriggerType}, StartDate:  {payload.StartDate}, " +
            $"Discription: {payload.Discription}, IsSalary: {payload.AccountNumber},Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Corporate User Create Salary Schedule. Action was carried out by a Corporate user"
        });

        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblTempCorporateSalarySchedule>(_data: new TblTempCorporateSalarySchedule(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetSalarySchedules")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<TblTempCorporateSalarySchedule>>> GetSalarySchedule()
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanViewSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }

        var companySchedules = await UnitOfWork.CorporateSalaryScheduleRepo.GetCorporateSalarySchedules(corporateCustomerDto.Id);

        return Ok(new ResponseDTO<List<TblCorporateSalarySchedule>>(_data: companySchedules, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpGet("GetPendingSalarySchedules")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<TblTempCorporateSalarySchedule>>> GetPendingSalarySchedules()
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }
        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanViewSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }
        var companySchedules = await UnitOfWork.TempCorporateSalaryScheduleRepo.GetPendingCorporateSalarySchedule(corporateCustomerDto.Id);
        return Ok(new ResponseDTO<List<TblTempCorporateSalarySchedule>>(_data: companySchedules, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("UpdateSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateSalarySchedule>> UpdateSalarySchedule(UpdateCorporateCustomerSalaryDtoRequest model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }

        var payload = new UpdateCorporateCustomerSalaryDto
        {
          Id = Encryption.DecryptGuid(model.Id),
          CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
          AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
          Frequency = Encryption.DecryptStrings(model.Frequency),
          TriggerType = Encryption.DecryptStrings(model.TriggerType),
          StartDate = Encryption.DecryptDateTime(model.StartDate),
          Discription = Encryption.DecryptStrings(model.Discription),
          IsSalary = Encryption.DecryptBooleans(model.IsSalary),
          WorkFlowId = Encryption.DecryptGuid(model.WorkFlowId),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };

        var validator = new UpdateCorporateSalaryScheduleValidation();
        var results = validator.Validate(payload);
        if (!results.IsValid)
        {
          return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
        }

        Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);

        var scheduleData = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (scheduleData == null)
        {
          return BadRequest("Invalid Schedule ID");
        }

        if (scheduleData.Status == (int)ProfileStatus.Deactivated)
        {
          return BadRequest("Action is not allow becouse the schedule is deactivated");
        }

        if (scheduleData.Status == (int)ProfileStatus.Pending)
        {
          return BadRequest("There is a pending request awaiting Approval");
        }

        var mapSchedule = this.MapUpdateRequestDtoToCorporateCustomerSalary(payload);
        var check = UnitOfWork.CorporateSalaryScheduleRepo.CheckDuplicate(mapSchedule);
        if (check.IsDuplicate)
        {
          return BadRequest(check.Message);
        }

        var mapTempSchedule = this.MapUpdateRequestDtoToTempCorporateCustomerSalary(payload);
        var checkTemp = UnitOfWork.TempCorporateSalaryScheduleRepo.CheckDuplicate(mapTempSchedule);
        if (checkTemp.IsDuplicate)
        {
          return BadRequest(check.Message);
        }
        scheduleData.Status = (int)ProfileStatus.Pending;
        mapTempSchedule.PreviousStatus = scheduleData.Status;
        mapTempSchedule.Status = (int)ProfileStatus.Modified;
        mapTempSchedule.Action = nameof(TempTableAction.Update).Replace("_", " ");

        if (payload.StartDate < DateTime.Now || DateTime.Now > payload.StartDate)
        {
          return BadRequest("Invalid Start Date");
        }


        if (_auth != AuthorizationType.Single_Signatory)
        {

          UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(scheduleData);
          UnitOfWork.TempCorporateSalaryScheduleRepo.Add(mapTempSchedule);
        }
        else
        {
          UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(scheduleData);
        }

        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Create).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {payload.AccountNumber}, " +
            $"Frequency: {payload.Frequency}, TriggerType: {payload.TriggerType}, StartDate:  {payload.StartDate}, " +
            $"Discription: {payload.Discription}, IsSalary: {payload.AccountNumber},Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Corporate User Update Salary Schedule. Action was carried out by a Corporate user"
        });
        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblCorporateSalarySchedule>(_data: new TblCorporateSalarySchedule(), success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("RequestSalaryScheduleApproval")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<CorporateProfileResponseDto>> RequestProfileApproval(SimpleActionDto model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.RequestCorporateUserProfileApproval))
        {
          return BadRequest("UnAuthorized Access");
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
        var entity = UnitOfWork.TempCorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (entity.InitiatorId != CorporateProfile.Id)
        {
          return BadRequest("This Request Was not Initiated By you");
        }

        if (!RequestApproval(entity, payload, out string errorMessage))
        {
          return StatusCode(400, errorMessage);
        }

        if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
        {
          return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data: new CorporateProfileResponseDto(), success: true, _message: Message.Success));
        }

        return Ok(new ResponseDTO<TblTempCorporateSalarySchedule>(_data: entity, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    [HttpPost("ApproveSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<CorporateProfileResponseDto>> ApproveProfile(SimpleActionDto model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanApprovedSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }

        if (string.IsNullOrEmpty(model.Id))
        {
          return BadRequest("Invalid Id");
        }
        var payload = new SimpleAction
        {
          Id = Encryption.DecryptGuid(model.Id),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };

        var entity = UnitOfWork.TempCorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (!ApprovedRequest(entity, payload, out string errorMessage))
        {
          return StatusCode(400, errorMessage);
        }

        if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
        {
          return Ok(new ResponseDTO<CorporateProfileResponseDto>(_data: new CorporateProfileResponseDto(), success: true, _message: Message.Success));
        }
        return Ok(new ResponseDTO<TblTempCorporateSalarySchedule>(_data: entity, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    [HttpPost("DeclineSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<CorporateProfileResponseDto>> DeclineProfile(AppAction model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanDeclineSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }

        if (model == null)
        {
          return BadRequest("Invalid Request");
        }

        var payload = new RequestCorporateProfileDto
        {
          Id = Encryption.DecryptGuid(model.Id),
          Reason = Encryption.DecryptStrings(model.Reason),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          MACAddress = Encryption.DecryptStrings(model.MACAddress),
        };
        //get profile by id
        var entity = UnitOfWork.TempCorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }


        if (!DeclineRequest(entity, payload, out string errorMessage))
        {
          return StatusCode(400, errorMessage);
        }

        if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
        {
          return Ok(new ResponseDTO<TblCorporateSalarySchedule>(_data: new TblCorporateSalarySchedule(), success: true, _message: Message.Success));
        }

        return Ok(new ResponseDTO<TblTempCorporateSalarySchedule>(_data: entity, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    [HttpPost("DeactivateSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> DeactivateProfile(AppAction model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanDeactivateSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }

        if (string.IsNullOrEmpty(model.Id))
        {
          return BadRequest("Invalid Id");
        }

        var payload = new SimpleAction
        {
          Id = Encryption.DecryptGuid(model.Id),
          Reason = Encryption.DecryptStrings(model.Reason),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          MACAddress = Encryption.DecryptStrings(model.MACAddress),
        };

        var entity = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (entity.Status == (int)ProfileStatus.Deactivated) return BadRequest("Schedule is already de-activated");

        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {entity.AccountNumber}, " +
            $"Frequency: {entity.Frequency}, TriggerType: {entity.TriggerType}, StartDate:  {entity.StartDate}, " +
            $"Discription: {entity.Discription}, IsSalary: {entity.IsSalary},Status: {nameof(ProfileStatus.Deactivated)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Deactivated Salary Schedule. Action was carried out by a Corporate user"
        });

        entity.Status = (int)ProfileStatus.Deactivated;
        UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(entity);
        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblCorporateSalarySchedule>(_data: entity, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("ReactivateSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> ReactivateProfile(AppAction model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanReactivateSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }

        var payload = new SimpleAction
        {
          Id = Encryption.DecryptGuid(model.Id),
          Reason = Encryption.DecryptStrings(model.Reason),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          MACAddress = Encryption.DecryptStrings(model.MACAddress),
        };
        var entity = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (entity.Status == (int)ProfileStatus.Active) return BadRequest("Salary Schedule is already activated");
        var status = (ProfileStatus)entity.Status;

        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Account Number: {entity.AccountNumber}, " +
            $"Frequency: {entity.Frequency}, TriggerType: {entity.TriggerType}, StartDate:  {entity.StartDate}, " +
            $"Discription: {entity.Discription}, IsSalary: {entity.IsSalary},Status: {nameof(ProfileStatus.Active)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Reactivated Salary Schedule. Action was carried out by a Corporate user"
        });

        entity.Status = (int)ProfileStatus.Active;
        UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(entity);
        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblCorporateSalarySchedule>(_data: entity, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    [HttpPost("InitiateSalarySchedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<TblCorporateCustomerEmployee>>> InitiatSchedule(AppAction model)
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

        if (CorporateProfile == null)
        {
          return BadRequest("Corporate customer Id could not be retrieved");
        }

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanInitiateSchedule))
        {
          if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
          {
            if (authorizationType != AuthorizationType.Single_Signatory)
            {
              return BadRequest("UnAuthorized Access");
            }
          }
          else
          {
            return BadRequest("Authorization type could not be determined!!!");
          }
        }


        var parallexSuspenseAccount = _config.GetValue<string>("NIPSBulkSuspenseAccount");
        var parallexSuspenseAccountName = _config.GetValue<string>("NIPSBulkSuspenseAccountName");
        var parallexInterSuspenseAccount = _config.GetValue<string>("NIPSInterBulkSuspenseAccount");
        var parallexInterSuspenseAccountName = _config.GetValue<string>("NIPSInterBulkSuspenseAccountName");
        var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
        var parralexBank = _config.GetValue<string>("ParralexBank");

        var payload = new SimpleAction
        {
          Id = Encryption.DecryptGuid(model.Id),
          Reason = Encryption.DecryptStrings(model.Reason),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          MACAddress = Encryption.DecryptStrings(model.MACAddress),
        };
        var entity = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        // var userName = $"{CorporateProfile.Username}{corporateCustomer.CustomerId}";
        // var validOTP = await _2FaService.TokenAuth(userName, payload.Otp);
        // if(validOTP.ResponseCode != "00"){
        //     return BadRequest($"2FA Service Error {validOTP.ResponseMessage}");
        // }

        if (entity.Status == (int)ProfileStatus.Deactivated) return BadRequest("Salary Schedule deactivated");

        if (entity.Status == (int)ProfileStatus.Pending) return BadRequest("Salary Schedule  is pending approval");

        if (entity.IsSalary > 0)
        {
          var checkIfHasUser = await _unitOfWork.CorporateEmployeeRepo.GetCorporateCustomerEmployees(corporateCustomerDto.Id);
          if (!checkIfHasUser.Any())
          {
            return BadRequest("no employee assign to this Salary Schedule ");
          }
        }
        else
        {
          var checkIfHasUser = await _unitOfWork.ScheduleBeneficairyRepo.GetScheduleBeneficiaries(entity);
          if (!checkIfHasUser.Any())
          {
            return BadRequest("no employee assign to payment Schedule");
          }
        }
        var bankList = await _apiService.GetBanks();
        if (bankList.ResponseCode != "00")
        {
          return BadRequest(bankList.ResponseMessage);
        }
        var feeCharges = await UnitOfWork.NipsFeeChargeRepo.ListAllAsync();

        if (Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth))
        {
          if (_auth != AuthorizationType.Single_Signatory)
          {

          }
        }
        var senderInfo = await _apiService.CustomerNameInquiry(entity.AccountNumber);
        if (senderInfo.ResponseCode != "00")
        {
          return BadRequest($"Source account number could not be verified -> {senderInfo.ResponseDescription}");
        }
        if (senderInfo.AccountStatus != "A")
        {
          return BadRequest($"Source account is not active transaction cannot be completed ");
        }

        if (senderInfo.FreezeCode != "N")
        {
          return BadRequest($"transaction cannot be completed  Source account Freeze ");
        }

        var tranDate = DateTime.Now;
        var checkIfIsSalary = entity.IsSalary == 1;
        var companyEmployees = new List<TblCorporateCustomerEmployee>();
        var scheduleEmployees = new List<TblCorporateSalaryScheduleBeneficiary>();
        var getFormatedBeneficiaries = new List<TblNipbulkCreditLog>();
        //var transferLog = this.PrepareBulkTransaction(senderInfo,entity,corporateCustomerDto,this._config);
        var transferLog = new TblNipbulkTransferLog
        {
          Id = Guid.NewGuid(),
          BatchId = Guid.NewGuid(),
        };


        if (checkIfIsSalary)
        {
          companyEmployees = await this.CorporeCustomerEmployees(entity);
          getFormatedBeneficiaries = await this.PrepareEmployeePayroll(transferLog, companyEmployees, entity, corporateCustomerDto, bankList, this._config, feeCharges);
          transferLog = this.PrepareBulkTransaction(transferLog, getFormatedBeneficiaries, senderInfo, entity, corporateCustomerDto, this._config);

        }
        else
        {
          scheduleEmployees = await this.CorporateBeneficairies(entity);
          getFormatedBeneficiaries = await this.ScheduleBeneficiaries(transferLog, scheduleEmployees, entity, corporateCustomerDto, bankList, this._config, feeCharges);
          transferLog = this.PrepareBulkTransaction(transferLog, getFormatedBeneficiaries, senderInfo, entity, corporateCustomerDto, this._config);

        }

        if (getFormatedBeneficiaries.Count != int.Parse(entity.NumberOfBeneficairy))
        {
          return BadRequest($"Number Of Beneficairy is not ");
        }


        if (_auth == AuthorizationType.Single_Signatory)
        {

          if (senderInfo.FreezeCode != "N")
          {
            return BadRequest($"transaction cannot be completed  Source account Freeze ");
          }

          if (checkIfIsSalary)
          {
            var bulkPosting = this.PrepareBulkTransactionPosting(getFormatedBeneficiaries, parallexBankCode, transferLog);
            var postBulkTransaction = FormatBulkTransaction(bulkPosting, transferLog);
            var postBulkIntraBankBulk = await _apiService.IntraBankBulkTransfer(postBulkTransaction);
            if (postBulkIntraBankBulk.ResponseCode != "00")
            {
              this.ProcessFailedBulkTransaction(postBulkIntraBankBulk, transferLog, parallexBankCode);
              this.AddAuditTrial(new AuditTrailDetail
              {
                Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated/Approved Salary Payment of " + transferLog.DebitAmount + " from " + transferLog.DebitAccountNumber,
                PreviousFieldValue = "",
                TransactionId = postBulkIntraBankBulk.TrnId,
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                UserId = CorporateProfile.Id,
                UserName = UserName,
                Description = "Corporate User Initiated/Approved Salary Payment Failed, this is process with bulk transaction API",
              });
              UnitOfWork.Complete();
              return BadRequest($"Transaction can not be completed at the moment -> {postBulkIntraBankBulk.ResponseMessage}:{postBulkIntraBankBulk.ResponseCode}");
            }
            else
            {
              if (transferLog.InterBankTotalAmount > 0)
              {
                var chargeResult = await this.ProcessBulkTransactionCharges(transferLog, parallexBankCode, parralexBank);
                var checkFeeTransaction = chargeResult.Where(ctx => ctx.ResponseCode != "00").ToList();
                if (checkFeeTransaction.Any())
                {
                  _logger.LogError("FEE TRANSACTION ERROR {0}", Formater.JsonType(chargeResult));
                }
                else
                {
                  this.AddAuditTrial(new AuditTrailDetail
                  {
                    Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                    NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Nip Charges of " + transferLog.TotalFee + "from " + transferLog.DebitAccountNumber,
                    PreviousFieldValue = "",
                    TransactionId = chargeResult[0].TransactionReference,
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    UserId = CorporateProfile.Id,
                    UserName = UserName,
                    Description = "Corporate User Initiated/Approved Salary Payment Fee Charges",
                  });
                  this.AddAuditTrial(new AuditTrailDetail
                  {
                    Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                    NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Vat Charges of " + transferLog.TotalVat + "from " + transferLog.DebitAccountNumber,
                    PreviousFieldValue = "",
                    TransactionId = chargeResult[2].TransactionReference,
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    UserId = CorporateProfile.Id,
                    UserName = UserName,
                    Description = "Corporate Initiated/Approved Salary Payment Vat Charges",
                  });
                }
              }
              this.ProcessSuccessfulBulkTransaction(postBulkIntraBankBulk, transferLog, parallexBankCode, tranDate);
              this.AddAuditTrial(new AuditTrailDetail
              {
                Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated/Approved Salary Payment of " + transferLog.DebitAmount + " from " + transferLog.DebitAccountNumber,
                PreviousFieldValue = "",
                TransactionId = postBulkIntraBankBulk.TrnId,
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                UserId = CorporateProfile.Id,
                UserName = UserName,
                Description = "Corporate User Initiated/Approved Salary Payment,  this is process with bulk transaction API",
              });
              UnitOfWork.NipBulkCreditLogRepo.AddRange(getFormatedBeneficiaries);
              UnitOfWork.Complete();
              return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful" });
            }
          }
          else
          {
            var bulkPostings = this.PrepareBulkTransactionPosting(getFormatedBeneficiaries, parallexBankCode, transferLog);
            var postBulkTransactions = FormatBulkTransaction(bulkPostings, transferLog);
            var bulkPostingResponse = await _apiService.IntraBankBulkTransfer(postBulkTransactions);
            if (bulkPostingResponse.ResponseCode != "00")
            {
              _logger.LogError("Transaction Schedule Initiation Error", Formater.JsonType(bulkPostingResponse));
              this.ProcessFailedBulkTransaction(bulkPostingResponse, transferLog, parallexBankCode);
              this.AddAuditTrial(new AuditTrailDetail
              {
                Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated transfer of " + transferLog.DebitAmount + " from " + transferLog.DebitAccountNumber,
                PreviousFieldValue = "",
                TransactionId = bulkPostingResponse.TrnId,
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                UserId = CorporateProfile.Id,
                UserName = UserName,
                Description = "Corporate User Initiated/Approved Bulk transfer Failed, this is process with bulk transaction API",
              });
              UnitOfWork.Complete();
              return BadRequest($"Transaction can not be completed at the moment -> {bulkPostingResponse.ResponseMessage}:{bulkPostingResponse.ResponseCode}");
            }
            else
            {
              if (transferLog.InterBankTotalAmount > 0)
              {
                var chargeResult = await this.ProcessBulkTransactionCharges(transferLog, parallexBankCode, parralexBank);
                var checkFeeTransaction = chargeResult.Where(ctx => ctx.ResponseCode != "00").ToList();
                if (checkFeeTransaction.Any())
                {
                  _logger.LogError("FEE TRANSACTION ERROR {0}", Formater.JsonType(chargeResult));
                }
                else
                {
                  this.AddAuditTrial(new AuditTrailDetail
                  {
                    Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                    NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Nip Charges of " + transferLog.TotalFee + "from " + transferLog.DebitAccountNumber,
                    PreviousFieldValue = "",
                    TransactionId = chargeResult[0].TransactionReference,
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    UserId = CorporateProfile.Id,
                    UserName = UserName,
                    Description = "Corporate  User Initiated/Approved Salary Payment Fee Charges",
                  });
                  this.AddAuditTrial(new AuditTrailDetail
                  {
                    Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                    NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Vat Charges of " + transferLog.TotalVat + "from " + transferLog.DebitAccountNumber,
                    PreviousFieldValue = "",
                    TransactionId = chargeResult[2].TransactionReference,
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    UserId = CorporateProfile.Id,
                    UserName = UserName,
                    Description = "Corporate User Initiated/Approved Salary Payment Vat Charges",
                  });
                }
              }
              this.ProcessSuccessfulBulkTransaction(bulkPostingResponse, transferLog, parallexBankCode, tranDate);
              this.AddAuditTrial(new AuditTrailDetail
              {
                Action = nameof(AuditTrailAction.Salary_Payment).Replace("_", " "),
                NewFieldValue = "Corporate User: " + CorporateProfile.FullName + "Initiated/Approved transfer of " + transferLog.DebitAmount + " from " + transferLog.DebitAccountNumber,
                PreviousFieldValue = "",
                TransactionId = bulkPostingResponse.TrnId,
                Ipaddress = payload.IPAddress,
                Macaddress = payload.MACAddress,
                HostName = payload.HostName,
                ClientStaffIpaddress = payload.ClientStaffIPAddress,
                UserId = CorporateProfile.Id,
                UserName = UserName,
                Description = "Corporate User Initiated/Approved Salary Payment,  this is process with bulk transaction API",
              });
              UnitOfWork.NipBulkCreditLogRepo.AddRange(getFormatedBeneficiaries);
              UnitOfWork.Complete();
              return Ok(new { Responsecode = "00", ResponseDescription = "Transaction Successful" });
            }
          }
        }

        var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId((Guid)entity?.WorkFlowId);
        if (!workflowHierarchies.Any())
        {
          return BadRequest("Authorizer has not been set");
        }

        var worflowValidation = this.ValidateWorkflowAccess((Guid)entity?.WorkFlowId, (decimal)transferLog?.DebitAmount);
        if (!worflowValidation.Status)
        {
          return BadRequest(worflowValidation.Message);
        }

        var ApprovalHistory = this.SetAuthorizationWorkFlow(workflowHierarchies, transferLog);
        transferLog.ApprovalStatus = 0;
        transferLog.ApprovalStage = 1;
        transferLog.ApprovalCount = workflowHierarchies.Count;
        transferLog.DateProccessed = tranDate;
        transferLog.WorkflowId = entity.WorkFlowId;
        UnitOfWork.NipBulkTransferLogRepo.Add(transferLog);
        UnitOfWork.NipBulkCreditLogRepo.AddRange(getFormatedBeneficiaries);
        UnitOfWork.CorporateApprovalHistoryRepo.AddRange(ApprovalHistory);
        UnitOfWork.Complete();
        this.SendForAuthorization(ApprovalHistory, transferLog);
        return Ok(new { Responsecode = "00", ResponseDescription = "Transaction has been forwarded for approval" });

      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
      }
    }

    private bool ApprovedRequest(TblTempCorporateSalarySchedule requestInfo, SimpleAction payload, out string errorMessage)
    {

      var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)requestInfo.CorporateCustomerId);
      if (corporateCustomerDto == null)
      {
        errorMessage = "Invalid Corporate Customer ID";
        return false;
      }

      if (requestInfo.Action == nameof(TempTableAction.Create).Replace("_", " "))
      {
        var mapSchedule = Mapper.Map<TblCorporateSalarySchedule>(requestInfo);
        var userStatus = UnitOfWork.CorporateSalaryScheduleRepo.CheckDuplicate(mapSchedule, false);
        if (userStatus.IsDuplicate)
        {
          errorMessage = userStatus.Message;
          return false;
        }

        var auditTrail = new TblAuditTrail
        {
          Id = Guid.NewGuid(),
          ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId} " +
                $" Account Number: {requestInfo.AccountNumber}, Description: {requestInfo.Discription}, NumberOfBeneficairy: {requestInfo.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Active)}",
          PreviousFieldValue = "",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Approved Newly Created payment schedule. Action was carried out by a Corporate user",
          TimeStamp = DateTime.Now
        };

        requestInfo.IsTreated = (int)ProfileStatus.Active;
        requestInfo.ApproverId = CorporateProfile.Id;
        requestInfo.ApproverUserName = UserName;
        requestInfo.DateApproved = DateTime.Now;
        mapSchedule.Sn = 0;
        mapSchedule.Status = (int)ProfileStatus.Active;
        UnitOfWork.TempCorporateSalaryScheduleRepo.UpdateSTempalarySchedule(requestInfo);
        UnitOfWork.CorporateSalaryScheduleRepo.Add(mapSchedule);
        UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        errorMessage = "";
        return true;
      }

      if (requestInfo.Action == nameof(TempTableAction.Update).Replace("_", " "))
      {
        var entity = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync((Guid)requestInfo.Id);
        var auditTrail = new TblAuditTrail
        {
          Id = Guid.NewGuid(),
          ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}," +
                $" Account Number: {requestInfo.AccountNumber}, Description: {requestInfo.Discription}, NumberOfBeneficairy: {requestInfo.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Active)}",
          PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}," +
                $" Account Number: {entity.AccountNumber}, Description: {entity.Discription}, NumberOfBeneficairy: {entity.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Pending)}",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Approved Payment Schedule Update. Action was carried out by a Corporate user",
          TimeStamp = DateTime.Now
        };

        entity.Discription = requestInfo.Discription;
        entity.NumberOfBeneficairy = requestInfo.NumberOfBeneficairy;
        entity.TriggerType = requestInfo.TriggerType;
        entity.Frequency = requestInfo.Frequency;
        entity.AccountNumber = requestInfo.AccountNumber;
        entity.IsSalary = requestInfo.IsSalary;
        entity.WorkFlowId = requestInfo.WorkFlowId;
        entity.StartDate = requestInfo.StartDate;
        var userStatus = UnitOfWork.CorporateSalaryScheduleRepo.CheckDuplicate(entity, true);
        if (userStatus.IsDuplicate)
        {
          errorMessage = userStatus.Message;
          return false;
        }
        requestInfo.IsTreated = (int)ProfileStatus.Active;
        entity.Status = (int)ProfileStatus.Active;
        requestInfo.ApproverId = CorporateProfile.Id;
        requestInfo.ApproverUserName = UserName;
        requestInfo.DateApproved = DateTime.Now;
        requestInfo.Reasons = payload.Reason;
        UnitOfWork.TempCorporateSalaryScheduleRepo.UpdateSTempalarySchedule(requestInfo);
        UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(entity);
        UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        errorMessage = userStatus.Message;
        return true;
      }
      errorMessage = "Unknow Request";
      return false;
    }
    private bool RequestApproval(TblTempCorporateSalarySchedule entity, SimpleAction payload, out string errorMessage)
    {
      var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
      if (corporateCustomerDto == null)
      {
        errorMessage = "Invalid Corporate Customer ID";
        return false;
      }

      if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
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
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId},Account Number: {entity.AccountNumber}, Description: {entity.Discription}, NumberOfBeneficairy: {entity.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Request Approval for new Corporate Profile. Action was carried out by a Bank user",
          TimeStamp = DateTime.Now
        };

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Pending;
        UnitOfWork.TempCorporateSalaryScheduleRepo.UpdateSTempalarySchedule(entity);
        UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        // notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
        errorMessage = "Request Approval Was Successful";
        return true;
      }

      if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
      {
        var schedule = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync(entity.Id);
        if (schedule == null)
        {
          errorMessage = "Profile wasn't Decline or modified initially";
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
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}" +
                    $" Account Number: {entity.AccountNumber}, Description: {entity.Discription}, NumberOfBeneficairy: {entity.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Request Approval for Corporate Profile Update. Action was carried out by a Corporate user",
          TimeStamp = DateTime.Now
        };

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);

        entity.Status = (int)ProfileStatus.Pending;
        schedule.Status = (int)ProfileStatus.Pending;
        UnitOfWork.TempCorporateSalaryScheduleRepo.UpdateSTempalarySchedule(entity);
        UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(schedule);
        UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        //notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
        errorMessage = "Request Approval Was Successful";
        return true;
      }

      errorMessage = "invalid Request";
      return false;
    }
    private bool DeclineRequest(TblTempCorporateSalarySchedule entity, RequestCorporateProfileDto payload, out string errorMessage)
    {
      var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);
      var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
      if (corporateCustomerDto == null)
      {
        errorMessage = "Invalid Corporate Customer ID";
        return false;
      }
      var notifyInfo = new EmailNotification
      {
        CustomerId = corporateCustomerDto.CustomerId,
        FullName = initiatorProfile.FullName,
        Email = initiatorProfile.Email,
        PhoneNumber = initiatorProfile.Phone1,
      };

      if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
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
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {initiatorProfile.FirstName}, " +
                $"Last Name: {initiatorProfile.LastName}, Username: {initiatorProfile.Username}, Email Address:  {initiatorProfile.Email}, " +
                $" Account Number: {entity.AccountNumber}, Description: {entity.Discription}, NumberOfBeneficairy: {entity.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Decline Approval for new Payment Schedules Profile. Action was carried out by a Corporate user",
          TimeStamp = DateTime.Now
        };

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Declined;
        entity.IsTreated = (int)ProfileStatus.Declined;
        entity.Reasons = payload.Reason;
        entity.ApproverId = CorporateProfile.Id;
        entity.ApproverUserName = UserName;
        entity.DateApproved = DateTime.Now;
        UnitOfWork.TempCorporateSalaryScheduleRepo.UpdateSTempalarySchedule(entity);
        UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        //notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
        errorMessage = "Decline Approval Was Successful";
        return true;
      }

      if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
      {
        var schedule = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync((Guid)entity.CorporateSalaryScheduleId);
        if (schedule == null)
        {
          errorMessage = "Profile wasn't Decline or modified initially";
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
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {initiatorProfile.FirstName}, " +
                $"Last Name: {initiatorProfile.LastName}, Username: {initiatorProfile.Username}, Email Address:  {initiatorProfile.Email}, " +
                $" Account Number: {entity.AccountNumber}, Description: {entity.Discription}, NumberOfBeneficairy: {entity.NumberOfBeneficairy}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Decline Approval for Corporate Profile Update. Action was carried out by a Bank user",
          TimeStamp = DateTime.Now
        };

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Declined;
        schedule.Status = (int)entity.PreviousStatus;
        entity.IsTreated = (int)ProfileStatus.Declined;
        entity.Reasons = payload.Reason;
        entity.ApproverId = CorporateProfile.Id;
        entity.ApproverUserName = UserName;
        entity.DateApproved = DateTime.Now;
        UnitOfWork.TempCorporateSalaryScheduleRepo.UpdateSTempalarySchedule(entity);
        UnitOfWork.CorporateSalaryScheduleRepo.UpdateCorporateSalarySchedule(schedule);
        UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        //notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
        errorMessage = "Decline Approval Was Successful";
        return true;
      }


      errorMessage = "invalid Request";
      return false;
    }
    private List<TblNipbulkCreditLog> PrepareBulkTransactionPosting(List<TblNipbulkCreditLog> transactionItem, string parallexBankCode, TblNipbulkTransferLog tranlg)
    {
      var interBankCreditItems = transactionItem.Where(ctx => ctx.CreditBankCode != parallexBankCode);
      var intraBankCreditItems = transactionItem.Where(ctx => ctx.CreditBankCode == parallexBankCode);
      var totalDebitAmountWithOutCharges = transactionItem.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
      var interBankTotalDebitAmount = interBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
      var intraBankTotalDebitAmount = intraBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.CreditAmount);
      var bulkSuspenseCreditItems = new List<TblNipbulkCreditLog>();
      if (interBankCreditItems.Any())
      {
        var totalVat = interBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.Vat);
        var totalFee = interBankCreditItems.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(ctx => ctx.Fee);
        bulkSuspenseCreditItems.AddRange(new[] {
                    new TblNipbulkCreditLog{
                        Id = Guid.NewGuid(),
                        TranLogId = tranlg.Id,
                        CreditAccountNumber = tranlg.IntreBankSuspenseAccountNumber,
                        CreditAccountName = tranlg.IntreBankSuspenseAccountName,
                        CreditAmount = Convert.ToDecimal(interBankTotalDebitAmount),
                        Narration = tranlg.Narration,
                        CreditStatus = 2,
                        BatchId = tranlg.BatchId,
                        NameEnquiryStatus = 0,
                        TryCount = 0,
                        CorporateCustomerId = CorporateProfile.CorporateCustomerId,
                        CreditDate = DateTime.Now,
                    },
                    new TblNipbulkCreditLog {
                        Id = Guid.NewGuid(),
                        TranLogId = tranlg.Id,
                        CreditAccountNumber = tranlg.IntreBankSuspenseAccountNumber,
                        CreditAccountName = tranlg.IntreBankSuspenseAccountName,
                        CreditAmount = Convert.ToDecimal(totalVat),
                        Narration = $"VCHG|{tranlg.Narration}"
                    },
                    new TblNipbulkCreditLog{
                        Id = Guid.NewGuid(),
                        TranLogId = tranlg.Id,
                        CreditAccountNumber = tranlg.IntreBankSuspenseAccountNumber,
                        CreditAccountName = tranlg.IntreBankSuspenseAccountName,
                        CreditAmount = Convert.ToDecimal(totalFee),
                        Narration = $"BCHG|{tranlg.Narration}"
                    }
                });
        tranlg.InterBankStatus = 0;
        tranlg.TotalFee = totalFee;
        tranlg.TotalVat = totalVat;
        tranlg.InterBankTotalAmount = interBankTotalDebitAmount;
      }
      if (intraBankCreditItems.Any())
      {
        bulkSuspenseCreditItems.Add(new TblNipbulkCreditLog
        {
          Id = Guid.NewGuid(),
          TranLogId = tranlg.Id,
          CreditAccountNumber = tranlg.SuspenseAccountNumber,
          CreditAccountName = tranlg.SuspenseAccountName,
          CreditAmount = Convert.ToDecimal(intraBankTotalDebitAmount),
          Narration = tranlg.Narration,
          CreditBankCode = parallexBankCode,
        });
        tranlg.IntraBankTotalAmount = intraBankTotalDebitAmount;
        tranlg.IntraBankStatus = 0;
      }
      tranlg.DebitAmount = totalDebitAmountWithOutCharges;
      tranlg.NoOfCredits = transactionItem.Where(ctx => ctx.NameEnquiryStatus == 1).Count();
      return bulkSuspenseCreditItems;
    }
    private List<TblCorporateApprovalHistory> SetAuthorizationWorkFlow(List<TblWorkflowHierarchy> workflowHierarchies, TblNipbulkTransferLog tranlg)
    {
      var tblCorporateApprovalHistories = new List<TblCorporateApprovalHistory>();
      foreach (var item in workflowHierarchies)
      {
        var toApproved = item.AuthorizationLevel == 1 ? 1 : 0;
        var corporateApprovalHistory = new TblCorporateApprovalHistory
        {
          Id = Guid.NewGuid(),
          LogId = tranlg.Id,
          Status = nameof(AuthorizationStatus.Pending),
          ApprovalLevel = item.AuthorizationLevel,
          ApproverName = item.ApproverName,
          Description = $"Authorizer {item.AuthorizationLevel}",
          Comment = "",
          UserId = item.ApproverId,
          ToApproved = toApproved,
          CorporateCustomerId = tranlg.CompanyId
        };
        UnitOfWork.CorporateApprovalHistoryRepo.Add(corporateApprovalHistory);
        tblCorporateApprovalHistories.Add(corporateApprovalHistory);
      }
      return tblCorporateApprovalHistories;
    }
    private TblNipbulkTransferLog PrepareBulkTransaction(TblNipbulkTransferLog tranlg, List<TblNipbulkCreditLog> benficiary, CustomerDataResponseDto senderInfo, TblCorporateSalarySchedule schedule, TblCorporateCustomer company, IConfiguration _config)
    {


      // var tranlg = new TblNipbulkTransferLog
      // {
      //     Id = Guid.NewGuid(),
      //     BatchId = Guid.NewGuid(),
      // };
      var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
      var DebitAmount = benficiary.Where(ctx => ctx.NameEnquiryStatus == 1).Sum(cux => cux.CreditAmount);
      var NoOfCredits = benficiary.Count(ctx => ctx.NameEnquiryStatus == 1);
      var interbank = benficiary.Where(ctx => ctx.CreditBankCode != parallexBankCode && ctx.NameEnquiryStatus == 1).ToList();
      var intrabank = benficiary.Where(ctx => ctx.CreditBankCode == parallexBankCode && ctx.NameEnquiryStatus == 1).ToList();
      if (interbank.Any())
      {
        var totalVat = interbank.Sum(ctx => ctx.Vat);
        var totalFee = interbank.Sum(ctx => ctx.Fee);
        tranlg.InterBankStatus = 0;
        tranlg.TotalFee = totalFee;
        tranlg.TotalVat = totalVat;
        tranlg.InterBankTotalAmount = interbank.Sum(ctx => ctx.CreditAmount);
      }
      if (intrabank.Any())
      {
        tranlg.IntraBankStatus = 0;
        tranlg.IntraBankTotalAmount = intrabank.Sum(ctx => ctx.CreditAmount);
      }
      tranlg.Sn = 0;
      tranlg.CompanyId = company.Id;
      tranlg.InitiatorId = CorporateProfile.Id;
      tranlg.DebitAccountName = senderInfo.AccountName;
      tranlg.DebitAccountNumber = senderInfo.AccountNumber;
      tranlg.DebitAmount = DebitAmount;
      tranlg.Narration = $"BP|{tranlg.BatchId}|{schedule.Discription}|{company.CompanyName}";
      tranlg.DateInitiated = DateTime.Now;
      tranlg.PostingType = "Bulk";
      tranlg.Currency = schedule.Currency;
      tranlg.TransactionStatus = 0;
      tranlg.TryCount = 0;
      tranlg.TransferType = nameof(TransactionType.Salary);
      tranlg.ApprovalStatus = 0;
      tranlg.ApprovalStage = 1;
      tranlg.InitiatorUserName = CorporateProfile.Username;
      tranlg.TransactionLocation = schedule.TransactionLocation;
      tranlg.SuspenseAccountName = _config.GetValue<string>("NIPSBulkSuspenseAccountName");
      tranlg.SuspenseAccountNumber = _config.GetValue<string>("NIPSBulkSuspenseAccount");
      tranlg.IntreBankSuspenseAccountName = _config.GetValue<string>("NIPSInterBulkSuspenseAccountName");
      tranlg.IntreBankSuspenseAccountNumber = _config.GetValue<string>("NIPSInterBulkSuspenseAccount");
      tranlg.TotalCredits = 0;
      tranlg.NoOfCredits = NoOfCredits;
      tranlg.InterBankTryCount = 0;
      tranlg.InterBankTotalCredits = 0;
      tranlg.Status = 0;
      return tranlg;

    }
    private async Task<TblNipbulkCreditLog> PrepareCreditbeneficiary(TblNipbulkTransferLog tranLog, TblCorporateCustomerEmployee employee, TblCorporateCustomer company, TblCorporateSalarySchedule schedule, BankListResponseData bankList, IReadOnlyList<TblFeeCharge> feeCharges, string parallexBankCode)
    {
      var items = await this.ValidateAccountNumber(employee.AccountNumber, employee.BankCode, bankList);
      items.TranLogId = tranLog.Id;
      items.CreditAmount = Convert.ToDecimal(employee.SalaryAmount);
      items.Narration = $"BP|{tranLog.BatchId}|{schedule.Discription}|{company.CompanyName}";
      items.BatchId = tranLog.BatchId;
      items.CorporateCustomerId = CorporateProfile.CorporateCustomerId;
      items.InitiateDate = DateTime.Now;
      if (items.CreditBankCode != parallexBankCode)
      {
        var nipsCharge = NipsCharge.Calculate(feeCharges, (decimal)employee.SalaryAmount);
        items.Fee = nipsCharge.Fee;
        items.Vat = nipsCharge.Vat;
      }
      return items;
    }
    private async Task<TblNipbulkCreditLog> ValidateAccountNumber(string accountNumber, string accountCode, BankListResponseData bankList)
    {
      var accountInfo = await _apiService.BankNameInquire(accountNumber, accountCode);
      var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == accountCode);
      var nipCreditInfo = new TblNipbulkCreditLog
      {
        Id = Guid.NewGuid()
      };
      if (accountInfo.ResponseCode != "00")
      {
        nipCreditInfo.CreditAccountNumber = accountInfo.AccountNumber;
        nipCreditInfo.CreditAccountName = accountInfo.AccountName;
        nipCreditInfo.CreditBankCode = accountCode;
        nipCreditInfo.CreditBankName = bank.InstitutionName;
        nipCreditInfo.CreditStatus = 2;
        nipCreditInfo.NameEnquiryRef = accountInfo.RequestId;
        nipCreditInfo.ResponseCode = accountInfo.ResponseCode;
        nipCreditInfo.ResponseMessage = accountInfo.ResponseMessage;
        nipCreditInfo.NameEnquiryStatus = 0;
        nipCreditInfo.TryCount = 0;
      }
      else
      {
        nipCreditInfo.CreditAccountNumber = accountInfo.AccountNumber;
        nipCreditInfo.CreditAccountName = accountInfo.AccountName;
        nipCreditInfo.CreditBankCode = accountCode;
        nipCreditInfo.CreditBankName = bank == null ? "Parallex Bank" : bank?.InstitutionName;
        nipCreditInfo.CreditStatus = 0;
        nipCreditInfo.NameEnquiryRef = accountInfo.RequestId;
        nipCreditInfo.ResponseCode = accountInfo.ResponseCode;
        nipCreditInfo.ResponseMessage = accountInfo.ResponseMessage;
        nipCreditInfo.NameEnquiryStatus = 1;
        nipCreditInfo.TryCount = 0;
      }
      return nipCreditInfo;
    }
    private TblCorporateSalarySchedule MapCreateRequestDtoToCorporateCustomerSalary(CreateCorporateCustomerSalaryDto payload)
    {
      var mapEmployee = Mapper.Map<TblCorporateSalarySchedule>(payload);
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.DateCreated = DateTime.Now;
      mapEmployee.Sn = 0;
      mapEmployee.Id = Guid.NewGuid();
      return mapEmployee;
    }
    private TblTempCorporateSalarySchedule MapCreateRequestDtoToTempCorporateCustomerSalary(CreateCorporateCustomerSalaryDto payload)
    {
      var mapEmployee = Mapper.Map<TblTempCorporateSalarySchedule>(payload);
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.DateCreated = DateTime.Now;
      mapEmployee.Sn = 0;
      mapEmployee.Id = Guid.NewGuid();
      return mapEmployee;
    }
    private TblCorporateSalarySchedule MapUpdateRequestDtoToCorporateCustomerSalary(UpdateCorporateCustomerSalaryDto payload)
    {
      var mapEmployee = Mapper.Map<TblCorporateSalarySchedule>(payload);
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.DateCreated = DateTime.Now;
      mapEmployee.Sn = 0;
      mapEmployee.Id = Guid.NewGuid();
      return mapEmployee;
    }
    private TblTempCorporateSalarySchedule MapUpdateRequestDtoToTempCorporateCustomerSalary(UpdateCorporateCustomerSalaryDto payload)
    {
      var mapEmployee = Mapper.Map<TblTempCorporateSalarySchedule>(payload);
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.DateCreated = DateTime.Now;
      mapEmployee.Sn = 0;
      mapEmployee.Id = Guid.NewGuid();
      return mapEmployee;
    }
    private List<TblNipbulkCreditLog> PrepareBulkTransactionCharges(TblNipbulkTransferLog creditLog, string parallexBankCode, string parralexBank)
    {
      var nipBulkCreditLogRepo = new List<TblNipbulkCreditLog>();
      nipBulkCreditLogRepo.AddRange(new[] {
            new TblNipbulkCreditLog{
                Id = Guid.NewGuid(),
                TranLogId = creditLog.Id,
                CreditAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
                CreditAccountName = creditLog.IntreBankSuspenseAccountName,
                CreditAmount = Convert.ToDecimal(creditLog.TotalVat),
                CreditBankCode = parallexBankCode,
                CreditBankName = parralexBank,
                Narration = $"VCHG|{creditLog.Narration}",
                CreditStatus = 2,
                BatchId = creditLog.BatchId,
                NameEnquiryStatus = 0,
                TryCount = 0,
                CorporateCustomerId = CorporateProfile.CorporateCustomerId,
                CreditDate = DateTime.Now,
            },
            new TblNipbulkCreditLog{
                Id = Guid.NewGuid(),
                TranLogId = creditLog.Id,
                CreditAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
                CreditAccountName = creditLog.IntreBankSuspenseAccountName,
                CreditAmount = Convert.ToDecimal(creditLog.TotalFee),
                CreditBankCode = parallexBankCode,
                CreditBankName = parralexBank,
                Narration = $"BCHG|{creditLog.Narration}",
                CreditStatus = 2,
                BatchId = creditLog.BatchId,
                NameEnquiryStatus = 0,
                TryCount = 0,
                CorporateCustomerId = CorporateProfile.CorporateCustomerId,
                CreditDate = DateTime.Now,
            }});
      return nipBulkCreditLogRepo;
    }
    private ValidationStatus ValidateWorkflowAccess(Guid? workflowId, decimal amount)
    {
      if (workflowId != null)
      {
        var workFlow = UnitOfWork.WorkFlowRepo.GetByIdAsync((Guid)workflowId);
        if (workFlow == null)
        {
          return new ValidationStatus { Status = false, Message = "Workflow is invalid" };
        }

        if (workFlow.Status != 1)
        {
          return new ValidationStatus { Status = false, Message = "Workflow selected is not active" };
        }

        var workflowHierarchies = UnitOfWork.WorkFlowHierarchyRepo.GetWorkflowHierarchiesByWorkflowId(workFlow.Id);
        if (workflowHierarchies.Count == 0)
        {
          return new ValidationStatus { Status = false, Message = "No Workflow Hierarchies found" };
        }
        if (workflowHierarchies.Count != workFlow.NoOfAuthorizers)
        {
          return new ValidationStatus { Status = false, Message = "Workflow Authorize is not valid " };
        }

      }
      return new ValidationStatus { Status = true, Message = "Validation OK" };
    }
    private bool DailyLimitExceeded(TblCorporateCustomer tblCorporateCustomer, decimal amount, out string errorMsg)
    {
      errorMsg = string.Empty;
      var customerDailyTransLimitHistory = _unitOfWork.TransactionHistoryRepo.GetTransactionHistory(tblCorporateCustomer.Id, DateTime.Now.Date);
      if (customerDailyTransLimitHistory != null)
      {
        if (tblCorporateCustomer.BulkTransDailyLimit != null)
        {
          if (customerDailyTransLimitHistory.BulkTransTotalAmount != null)
          {
            decimal amtTransferable = (decimal)tblCorporateCustomer.BulkTransDailyLimit - (decimal)customerDailyTransLimitHistory.BulkTransTotalAmount;

            if (amtTransferable < amount)
            {
              if (amtTransferable <= 0)
              {
                errorMsg = $"You have exceeded your daily bulk transaction limit Which is {Helper.formatCurrency(tblCorporateCustomer.BulkTransDailyLimit)}";
                return true;
              }
              errorMsg = $"Transaction amount {Helper.formatCurrency(amount)} has exceeded the maximum daily transaction limit {Helper.formatCurrency(tblCorporateCustomer.BulkTransDailyLimit)} for your organisation. You can only transfer {Helper.formatCurrency(amtTransferable)} for the rest of the day";
              return true;
            }
          }
        }
      }
      return false;
    }
    private void AddAuditTrial(AuditTrailDetail info)
    {
      var auditTrail = new TblAuditTrail
      {
        Id = Guid.NewGuid(),
        ActionCarriedOut = info.Action,
        Ipaddress = info.Ipaddress,
        Macaddress = info.Macaddress,
        HostName = info.HostName,
        ClientStaffIpaddress = info.ClientStaffIpaddress,
        NewFieldValue = info.NewFieldValue,
        PreviousFieldValue = info.PreviousFieldValue,
        TransactionId = "",
        UserId = info.UserId,
        Username = info.UserName,
        Description = $"{info.Description}",
        TimeStamp = DateTime.Now
      };
      UnitOfWork.AuditTrialRepo.Add(auditTrail);
    }
    private void SendForAuthorization(List<TblCorporateApprovalHistory> workflowHierarchies, TblNipbulkTransferLog tranlg)
    {
      var firstApproval = workflowHierarchies.First(ctx => ctx.ApprovalLevel == 1);
      var corporateUser = UnitOfWork.CorporateProfileRepo.GetByIdAsync(firstApproval.UserId.Value);
      var initiatorName = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)tranlg.InitiatorId);
      ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.RequestApproval(corporateUser.Email, initiatorName.FullName, string.Format("{0:0.00}", tranlg.DebitAmount))));
    }
    private static BulkIntrabankTransactionModel FormatBulkTransaction(List<TblNipbulkCreditLog> bulkTransaction, TblNipbulkTransferLog creditLog)
    {
      var narrationTuple = creditLog.Narration.Length > 50 ? Tuple.Create(creditLog.Narration[..50], creditLog.Narration[50..]) : Tuple.Create(creditLog.Narration, "");
      var tranDate = DateTime.Now;
      var creditItems = new List<PartTrnRec>();
      var beneficiary = new PartTrnRec
      {
        AcctId = creditLog.DebitAccountNumber,
        CreditDebitFlg = "D",
        TrnAmt = creditLog.DebitAmount.ToString(),
        currencyCode = "NGN",
        TrnParticulars = narrationTuple.Item1,
        ValueDt = tranDate.ToString("MM/dd/yyyy HH:mm:ss"),
        PartTrnRmks = Generate16DigitNumber.Create16DigitString(),
        REFNUM = "",
        RPTCODE = "",
        TRANPARTICULARS2 = narrationTuple.Item2
      };
      creditItems.Add(beneficiary);
      foreach (var item in bulkTransaction)
      {
        var tranNarration = item.Narration.Length > 50 ? Tuple.Create(item.Narration[..50], item.Narration[50..]) : Tuple.Create(item.Narration, "");
        var creditBeneficiary = new PartTrnRec
        {
          AcctId = item.CreditAccountNumber,
          CreditDebitFlg = "C",
          TrnAmt = item.CreditAmount.ToString(),
          currencyCode = "NGN",
          TrnParticulars = tranNarration.Item1,
          ValueDt = tranDate.ToString("MM/dd/yyyy HH:mm:ss"),
          PartTrnRmks = Generate16DigitNumber.Create16DigitString(),
          REFNUM = "",
          RPTCODE = "",
          TRANPARTICULARS2 = tranNarration.Item2
        };
        creditItems.Add(creditBeneficiary);
      };
      var intraBankBulkTransfer = new BulkIntrabankTransactionModel
      {
        BankId = "01",
        TrnType = "T",
        TrnSubType = "CI",
        RequestID = Generate16DigitNumber.Create16DigitString(),
        PartTrnRec = creditItems,
      };
      return intraBankBulkTransfer;
    }
    private async Task<List<IntraBankTransferResponse>> ProcessBulkTransactionCharges(TblNipbulkTransferLog creditLog, string parallexBankCode, string parralexBank)
    {
      var responseResult = new List<IntraBankTransferResponse>();
      var bulkTransaction = this.PrepareBulkTransactionCharges(creditLog, parallexBankCode, parralexBank);
      foreach (var item in bulkTransaction)
      {
        var narrationTuple = item.Narration.Length > 50 ? Tuple.Create(item.Narration[..50], item.Narration[50..]) : Tuple.Create(item.Narration, "");
        var date = DateTime.Now;
        var transfer = new IntraBankPostDto
        {
          AccountToDebit = creditLog.DebitAccountNumber,
          UserName = CorporateProfile.Username,
          Channel = "2",
          TransactionLocation = creditLog.TransactionLocation,
          IntraTransferDetails = new List<IntraTransferDetail>{
                        new IntraTransferDetail {
                            TransactionReference = Generate16DigitNumber.Create16DigitString(),
                            TransactionDate = date.ToString("MM/dd/yyyy HH:mm:ss"),
                            BeneficiaryAccountName = creditLog.IntreBankSuspenseAccountName,
                            BeneficiaryAccountNumber = creditLog.IntreBankSuspenseAccountNumber,
                            Amount = item.CreditAmount,
                            Narration = narrationTuple.Item1
                        }
                    }
        };
        var transferResult = await _apiService.IntraBankTransfer(transfer);
        if (transferResult.ResponseCode != "00")
        {
          responseResult.Add(transferResult);
        }
        else
        {
          responseResult.Add(transferResult);
        }
      }
      return responseResult;
    }
    private async Task<List<TblNipbulkCreditLog>> ScheduleBeneficiaries(TblNipbulkTransferLog tranlg, List<TblCorporateSalaryScheduleBeneficiary> beneficiaries, TblCorporateSalarySchedule schedule, TblCorporateCustomer company, BankListResponseData bankList, IConfiguration _config, IReadOnlyList<TblFeeCharge> feeCharges)
    {
      var beneficairiesList = new List<TblNipbulkCreditLog>();
      //var tranlg = PrepareBulkTransaction(schedule,company,_config);
      var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
      foreach (var beneficiary in beneficiaries)
      {
        var items = await this.PrepareScheduleBeneficiary(beneficiary, parallexBankCode, tranlg, bankList, feeCharges);
        if (items != null)
        {
          items.Narration = $"BP|{tranlg.BatchId}|{schedule.Discription}|{company.CompanyName}";
          beneficairiesList.Add(items);
        }
      }
      return beneficairiesList;
    }
    private async Task<TblNipbulkCreditLog> PrepareScheduleBeneficiary(TblCorporateSalaryScheduleBeneficiary item, string parallexBankCode, TblNipbulkTransferLog tranlg, BankListResponseData bankList, IReadOnlyList<TblFeeCharge> feeCharges)
    {
      var employee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync((Guid)item.EmployeeId);
      if (employee != null)
      {
        return null;
      }
      var items = await this.ValidateAccountNumber(employee.AccountNumber, employee.BankCode, bankList);
      items.TranLogId = tranlg.Id;
      items.CreditAmount = Convert.ToDecimal(item.Amount);
      items.BatchId = tranlg.BatchId;
      items.CorporateCustomerId = CorporateProfile.CorporateCustomerId;
      items.InitiateDate = DateTime.Now;
      if (items.CreditBankCode != parallexBankCode)
      {
        var nipsCharge = NipsCharge.Calculate(feeCharges, (decimal)item.Amount);
        items.Fee = nipsCharge.Fee;
        items.Vat = nipsCharge.Vat;
      }
      return items;
    }
    private void ProcessFailedBulkTransaction(BulkIntraBankTransactionResponse postBulkIntraBankBulk, TblNipbulkTransferLog tranlg, string parralexBank)
    {
      _logger.LogError("TRANSACTION ERROR {0}, {1}, {2}", Formater.JsonType(postBulkIntraBankBulk.ResponseCode), Formater.JsonType(postBulkIntraBankBulk.ResponseMessage), Formater.JsonType(postBulkIntraBankBulk.ErrorDetail));

      if (tranlg.InterBankTotalAmount > 0)
      {
        UnitOfWork.TransactionRepo.Add(new TblTransaction
        {
          Id = Guid.NewGuid(),
          TranAmout = tranlg.InterBankTotalAmount,
          DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
          DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
          DesctionationBank = parralexBank,
          TranType = "bulk",
          TransactionStatus = nameof(TransactionStatus.Failed),
          Narration = $"{tranlg.Narration}|inter",
          SourceAccountName = tranlg.DebitAccountName,
          SourceAccountNo = tranlg.DebitAccountNumber,
          SourceBank = parralexBank,
          CustAuthId = CorporateProfile.Id,
          Channel = "WEB",
          TransactionReference = postBulkIntraBankBulk.TrnId,
          ResponseCode = postBulkIntraBankBulk.ResponseCode,
          ResponseDescription = postBulkIntraBankBulk.ResponseMessage,
          TranDate = DateTime.Now,
          CorporateCustomerId = CorporateProfile.CorporateCustomerId,
          BatchId = tranlg.BatchId
        });
      }
      if (tranlg.IntraBankTotalAmount > 0)
      {
        UnitOfWork.TransactionRepo.Add(new TblTransaction
        {
          Id = Guid.NewGuid(),
          TranAmout = tranlg.IntraBankTotalAmount,
          DestinationAcctName = tranlg.SuspenseAccountName,
          DestinationAcctNo = tranlg.SuspenseAccountNumber,
          DesctionationBank = parralexBank,
          TranType = "bulk",
          TransactionStatus = nameof(TransactionStatus.Failed),
          Narration = $"{tranlg.Narration}|intra",
          SourceAccountName = tranlg.DebitAccountName,
          SourceAccountNo = tranlg.DebitAccountNumber,
          SourceBank = parralexBank,
          CustAuthId = CorporateProfile.Id,
          Channel = "WEB",
          TransactionReference = postBulkIntraBankBulk.TrnId,
          ResponseCode = postBulkIntraBankBulk.ResponseCode,
          ResponseDescription = postBulkIntraBankBulk.ResponseMessage,
          TranDate = DateTime.Now,
          CorporateCustomerId = CorporateProfile.CorporateCustomerId,
          BatchId = tranlg.BatchId
        });
      }
      tranlg.ResponseCode = postBulkIntraBankBulk.ResponseCode;
      tranlg.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
      tranlg.ErrorDetail = Formater.JsonType(postBulkIntraBankBulk.ErrorDetail);
      tranlg.Status = 2;
      tranlg.TransactionStatus = 2;
      tranlg.ApprovalStatus = 1;
      tranlg.TransactionReference = postBulkIntraBankBulk.TrnId;
      UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
    }
    private void ProcessSuccessfulBulkTransaction(BulkIntraBankTransactionResponse postBulkIntraBankBulk, TblNipbulkTransferLog tranlg, string parralexBank, DateTime tranDate)
    {

      if (tranlg.InterBankTotalAmount > 0)
      {
        UnitOfWork.TransactionRepo.AddRange(new[] {
                    new TblTransaction {
                        Id = Guid.NewGuid(),
                        TranAmout = tranlg.InterBankTotalAmount,
                        DestinationAcctName = tranlg.IntreBankSuspenseAccountName,
                        DestinationAcctNo = tranlg.IntreBankSuspenseAccountNumber,
                        DesctionationBank = parralexBank,
                        TranType = "bulk",
                        TransactionStatus = nameof(TransactionStatus.Successful),
                        Narration = $"{tranlg.Narration}|inter",
                        SourceAccountName = tranlg.DebitAccountName,
                        SourceAccountNo = tranlg.DebitAccountNumber,
                        SourceBank = parralexBank,
                        CustAuthId = CorporateProfile.Id,
                        Channel = "WEB",
                        TransactionReference = postBulkIntraBankBulk.TrnId,
                        ResponseCode = postBulkIntraBankBulk.ResponseCode,
                        ResponseDescription= postBulkIntraBankBulk.ResponseMessage,
                        TranDate = DateTime.Now,
                        CorporateCustomerId = CorporateProfile.CorporateCustomerId,
                        BatchId = tranlg.BatchId
                    },
                });
      }
      if (tranlg.IntraBankTotalAmount > 0)
      {
        UnitOfWork.TransactionRepo.Add(new TblTransaction
        {
          Id = Guid.NewGuid(),
          TranAmout = tranlg.IntraBankTotalAmount,
          DestinationAcctName = tranlg.SuspenseAccountName,
          DestinationAcctNo = tranlg.SuspenseAccountNumber,
          DesctionationBank = parralexBank,
          TranType = "bulk",
          TransactionStatus = nameof(TransactionStatus.Successful),
          Narration = $"{tranlg.Narration}|intra",
          SourceAccountName = tranlg.DebitAccountName,
          SourceAccountNo = tranlg.DebitAccountNumber,
          SourceBank = parralexBank,
          CustAuthId = CorporateProfile.Id,
          Channel = "WEB",
          TransactionReference = postBulkIntraBankBulk.TrnId,
          ResponseCode = postBulkIntraBankBulk.ResponseCode,
          ResponseDescription = postBulkIntraBankBulk.ResponseMessage,
          TranDate = DateTime.Now,
          CorporateCustomerId = CorporateProfile.CorporateCustomerId,
          BatchId = tranlg.BatchId
        });
      }
      tranlg.ResponseCode = postBulkIntraBankBulk.ResponseCode;
      tranlg.ResponseDescription = postBulkIntraBankBulk.ResponseMessage;
      tranlg.Status = 1;
      tranlg.DateProccessed = tranDate;
      tranlg.ApprovalStatus = 1;
      tranlg.ApprovalCount = 1;
      tranlg.ApprovalStage = 1;
      tranlg.TransactionStatus = 0;
      tranlg.TransactionReference = postBulkIntraBankBulk.TrnId;
      UnitOfWork.NipBulkTransferLogRepo.Add(tranlg);
    }
    private async Task<List<TblCorporateCustomerEmployee>> CorporeCustomerEmployees(TblCorporateSalarySchedule customer)
    {
      return await UnitOfWork.CorporateEmployeeRepo.GetCorporateCustomerEmployees((Guid)customer.CorporateCustomerId);
    }
    private async Task<List<TblCorporateSalaryScheduleBeneficiary>> CorporateBeneficairies(TblCorporateSalarySchedule entity)
    {
      return await UnitOfWork.ScheduleBeneficairyRepo.GetScheduleBeneficiaries(entity);
    }
    private async Task<List<TblNipbulkCreditLog>> PrepareEmployeePayroll(TblNipbulkTransferLog tranlg, List<TblCorporateCustomerEmployee> employees, TblCorporateSalarySchedule schedule, TblCorporateCustomer company, BankListResponseData bankList, IConfiguration _config, IReadOnlyList<TblFeeCharge> feeCharges)
    {
      var transactionItem = new List<TblNipbulkCreditLog>();
      var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
      foreach (var employee in employees)
      {
        var item = await this.PrepareCreditbeneficiary(tranlg, employee, company, schedule, bankList, feeCharges, parallexBankCode);
        transactionItem.Add(item);
      }
      return transactionItem;
    }

    private static bool ValidatePayload(out string errorMessage, CreateCorporateCustomerSalaryDto create = null, UpdateCorporateCustomerSalaryDto update = null)
    {

      if (create != null)
      {
        if (string.IsNullOrEmpty(create.Frequency) || !new ReqEx().AlphabetOnly.IsMatch(create.Frequency))
        {
          errorMessage = "Frequency is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.TriggerType) || !new ReqEx().AlphabetOnly.IsMatch(create.TriggerType))
        {
          errorMessage = "Trigger Type is require";
          return true;
        }
        if (create.StartDate == null || create.StartDate < DateTime.Now || DateTime.Now > create.StartDate)
        {
          errorMessage = "Valid Start Date is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.Discription))
        {
          errorMessage = "Discription is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.NumberOfBeneficairy))
        {
          errorMessage = "Number Of Beneficairy";
          return true;
        }

        var isValid = Guid.TryParse(create.CorporateCustomerId.ToString(), out _);
        if (!isValid)
        {
          errorMessage = "invalid Corporate Customer Id";
          return true;
        }

        errorMessage = "Ok";
        return false;
      }
      if (update != null)
      {
        if (string.IsNullOrEmpty(create.Frequency) || new ReqEx().AlphabetOnly.IsMatch(create.Frequency))
        {
          errorMessage = "Frequency is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.TriggerType) || new ReqEx().AlphabetOnly.IsMatch(create.TriggerType))
        {
          errorMessage = "Trigger Type is require";
          return true;
        }
        if (create.StartDate == null || create.StartDate < DateTime.Now || DateTime.Now > create.StartDate)
        {
          errorMessage = "Valid Start Date is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.Discription))
        {
          errorMessage = "Discription is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.NumberOfBeneficairy))
        {
          errorMessage = "Number Of Beneficairy";
          return true;
        }

        if (string.IsNullOrEmpty(create.Discription))
        {
          errorMessage = "Description is require";
          return true;
        }

        var isValid = Guid.TryParse(create.CorporateCustomerId.ToString(), out _);
        if (!isValid)
        {
          errorMessage = "invalid Corporate Customer Id";
          return true;
        }

        var isValidId = Guid.TryParse(update.Id.ToString(), out _);
        if (!isValidId)
        {
          errorMessage = "invalid Shedule Id";
          return true;
        }

        errorMessage = "Ok";
        return false;
      }
      errorMessage = "invalid Request";
      return true;
    }



  }
}

