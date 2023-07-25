
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateSalarySchedule._ScheduleBeneficiary.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CIB.CorporateAdmin.Controllers
{
  [ApiController]
  [Route("api/CorporateAdmin/v1/[controller]")]
  public class ComporateScheduleBeneficiaryController : BaseAPIController
  {
    private readonly ILogger _logger;
    public ComporateScheduleBeneficiaryController(ILogger<ComporateCustomerEmployeeController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
    {
      _logger = logger;
    }

    [HttpPost("AddScheduleBeneficiary")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> AddBeneficiary(CreateBeneficiaryRequest model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateBeneficiary))
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

        if (string.IsNullOrEmpty(model.Beneficiaries))
        {
          return BadRequest("beneficiary is empty");
        }

        if (string.IsNullOrEmpty(model.ScheduleId))
        {
          return BadRequest("beneficiary is empty");
        }
        var payload = new CreateBeneficiaryRequestDto
        {
          CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
          ScheduleId = Encryption.DecryptGuid(model.ScheduleId),
          Beneficiaries = JsonConvert.DeserializeObject<List<Beneficiary>>(Encryption.DecryptStrings(model.Beneficiaries)),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };


        if (!model.Beneficiaries.Any())
        {
          return BadRequest("No Beneficiary Selected");
        }

        var beneficiaryList = new List<TblCorporateSalaryScheduleBeneficiary>();
        var removeBeneficiaryList = new List<TblCorporateSalaryScheduleBeneficiary>();
        foreach (var beneficiary in payload.Beneficiaries)
        {
          var getEmployee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(beneficiary.EmployeeId.Value);
          if (getEmployee == null)
          {
            return BadRequest($"No Employee  with the id {beneficiary.EmployeeId} does not exist");
          }
          var checkDuplicateEmployee = UnitOfWork.ScheduleBeneficairyRepo.CheckScheduleBeneficiary(corporateCustomerDto.Id, (Guid)payload.ScheduleId, beneficiary.EmployeeId.Value);
          if (checkDuplicateEmployee != null)
          {
            removeBeneficiaryList.Add(checkDuplicateEmployee);
            beneficiaryList.Add(new TblCorporateSalaryScheduleBeneficiary
            {
              Id = Guid.NewGuid(),
              CorporateCustomerId = corporateCustomerDto.Id,
              EmployeeId = beneficiary?.EmployeeId,
              Amount = beneficiary?.Amount,
              ScheduleId = payload.ScheduleId,
            });
          }
          else
          {
            beneficiaryList.Add(new TblCorporateSalaryScheduleBeneficiary
            {
              Id = Guid.NewGuid(),
              CorporateCustomerId = corporateCustomerDto.Id,
              EmployeeId = beneficiary?.EmployeeId,
              Amount = beneficiary?.Amount,
              ScheduleId = payload.ScheduleId,
            });
          }
        }

        if (removeBeneficiaryList.Count == 0)
        {
          UnitOfWork.ScheduleBeneficairyRepo.RemoveRange(removeBeneficiaryList);
          UnitOfWork.Complete();
        }

        var auditTrail = new TblAuditTrail
        {
          Id = Guid.NewGuid(),
          ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}," +
            "Beneficiaries: ${JsonConvert.SerializeObject(beneficiaryList)}",
          PreviousFieldValue = "",
          TransactionId = "",
          UserId = CorporateProfile.Id,
          Username = UserName,
          Description = $"Corporate User Create Salary Schedule beneficairy. Action was carried out by a Corporate user",
          TimeStamp = DateTime.Now
        };
        UnitOfWork.ScheduleBeneficairyRepo.AddRange(beneficiaryList);
        UnitOfWork.Complete();
        return Ok(true);
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

    [HttpPost("RemoveScheduleBeneficiary")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<bool> RequestProfileApproval(UpdateABeneficiary model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanRemoveBeneficiary))
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
        var payload = new UpdateBeneficiaryDto
        {
          Id = Encryption.DecryptGuid(model.Id),
          CorporateCustomerId = CorporateProfile.CorporateCustomerId,
          Beneficiaries = JsonConvert.DeserializeObject<List<Beneficiary>>(Encryption.DecryptStrings(model.Beneficiaries)),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)

        };

        var entity = UnitOfWork.ScheduleBeneficairyRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }
        var beneficiaryList = new List<TblCorporateSalaryScheduleBeneficiary>();
        var removeBeneficiaryList = new List<TblCorporateSalaryScheduleBeneficiary>();
        foreach (var beneficiary in payload.Beneficiaries)
        {

          var getEmployee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(beneficiary.EmployeeId.Value);
          if (getEmployee == null)
          {
            return BadRequest($"No Employee  with the id {beneficiary.EmployeeId} does not exist");
          }
          var checkDuplicateEmployee = UnitOfWork.ScheduleBeneficairyRepo.CheckScheduleBeneficiary(corporateCustomerDto.Id, payload.Id, beneficiary.EmployeeId.Value);
          if (checkDuplicateEmployee != null)
          {
            removeBeneficiaryList.Add(checkDuplicateEmployee);
            beneficiaryList.Add(new TblCorporateSalaryScheduleBeneficiary
            {
              Id = Guid.NewGuid(),
              CorporateCustomerId = payload.CorporateCustomerId,
              EmployeeId = beneficiary.EmployeeId.Value,
              Amount = beneficiary.Amount.Value,
              ScheduleId = payload.Id,
            });
          }
          else
          {
            beneficiaryList.Add(new TblCorporateSalaryScheduleBeneficiary
            {
              Id = Guid.NewGuid(),
              CorporateCustomerId = payload.CorporateCustomerId,
              EmployeeId = beneficiary.EmployeeId.Value,
              Amount = beneficiary.Amount.Value,
              ScheduleId = payload.Id,
            });
          }
        }

        if (removeBeneficiaryList.Any())
        {
          UnitOfWork.ScheduleBeneficairyRepo.RemoveRange(removeBeneficiaryList);
          UnitOfWork.Complete();
        }
        UnitOfWork.ScheduleBeneficairyRepo.AddRange(beneficiaryList);
        UnitOfWork.Complete();
        return Ok(true);
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

    [HttpGet("GetScheduleBeneficiaries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ListResponseDTO<ScheduleBeneficiaryResponse>>> GetPendingApproval(string scheduleId)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanViewBeneficiary))
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
        if (string.IsNullOrEmpty(scheduleId))
        {
          return BadRequest("Schedule Id is require");
        }


        var shedule = Encryption.DecryptGuid(scheduleId);
        var getSalarySchedule = UnitOfWork.CorporateSalaryScheduleRepo.GetByIdAsync(shedule);
        if (getSalarySchedule == null)
        {
          return BadRequest("Invalid id. Salary Schedule not found");
        }
        var beneficaries = await UnitOfWork.ScheduleBeneficairyRepo.GetScheduleBeneficiaryDetails(getSalarySchedule);

        return Ok(new ListResponseDTO<ScheduleBeneficiaryResponse>(_data: beneficaries, success: true, _message: Message.Success));
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        }
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }
  }
}