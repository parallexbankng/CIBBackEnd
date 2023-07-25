
using CIB.Core.Common.Interface;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using CIB.Core.Common.Response;
using CIB.Core.Utils;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Common.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using CIB.Core.Common;
using CIB.Core.Services.File;
using CIB.Core.Services.Api;
using CIB.Core.Modules.TempCorporateSalarySchedule._TempCorporateEmployee.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Modules.CorporateProfile.Dto;
using System.Text.RegularExpressions;

namespace CIB.CorporateAdmin.Controllers
{
  [ApiController]
  [Route("api/CorporateAdmin/v1/[controller]")]
  public class ComporateCustomerEmployeeController : BaseAPIController
  {
    private readonly ILogger _logger;
    private readonly IFileService _fileService;
    private readonly IApiService _apiService;

    public ComporateCustomerEmployeeController(IApiService apiService, IFileService fileService, ILogger<ComporateCustomerEmployeeController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService) : base(unitOfWork, mapper, accessor, authService)
    {
      _logger = logger;
      _fileService = fileService;
      _apiService = apiService;
    }

    [HttpPost("CreateCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> CreateCorporateEmployee(CreateCorporateEmployeeRequest model)
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

        var payload = new CreateCorporateEmployeeDto
        {
          CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
          FirstName = Encryption.DecryptStrings(model.FirstName),
          LastName = Encryption.DecryptStrings(model.LastName),
          StaffId = Encryption.DecryptStrings(model.StaffId),
          Department = Encryption.DecryptStrings(model.Department),
          AccountName = Encryption.DecryptStrings(model.AccountName),
          AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
          BankCode = Encryption.DecryptStrings(model.BankCode),
          SalaryAmount = Encryption.DecryptDecimals(model.SalaryAmount),
          Description = Encryption.DecryptStrings(model.Description),
          GradeLevel = Encryption.DecryptStrings(model.GradeLevel),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateStaff))
        {
          if (_auth != AuthorizationType.Single_Signatory)
          {
            return BadRequest("UnAuthorized Access");
          }

        }

        if (ValidatePaload(out errormsg, payload, null))
        {
          return BadRequest(errormsg);
        }

        var mapTempEmployee = this.MapCreateRequestDtoToCorporateEmployee(payload);
        var checkTempResult = UnitOfWork.TempCorporateEmployeeRepo.CheckDuplicate(mapTempEmployee);
        if (checkTempResult.IsDuplicate)
        {
          return BadRequest(checkTempResult.Message);
        }

        var mapEmployee = Mapper.Map<TblCorporateCustomerEmployee>(mapTempEmployee);
        var checkResult = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(mapEmployee);
        if (checkResult.IsDuplicate)
        {
          return BadRequest(checkResult.Message);
        }


        if (_auth == AuthorizationType.Single_Signatory)
        {
          mapEmployee.Status = (int)ProfileStatus.Active;
          UnitOfWork.CorporateEmployeeRepo.Add(mapEmployee);
        }
        else
        {
          mapTempEmployee.Action = nameof(TempTableAction.Create).Replace("_", " ");
          UnitOfWork.TempCorporateEmployeeRepo.Add(mapTempEmployee);
        }

        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Create).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Schedule Description: {payload.FirstName}, " +
            $"Last Name: {payload.LastName}, StaffId: {payload.StaffId}, Department:  {payload.Department}, " +
            $"AccountName: {payload.AccountName}, AccountNumber: {payload.AccountNumber}, BankCode: {payload.BankCode}, " +
            $"SalaryAmount: {payload.SalaryAmount}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Corporate User Create Corporate Employee. Action was carried out by a Corporate user"
        });

        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblTempCorporateCustomerEmployee>(_data: mapTempEmployee, success: true, _message: Message.Success));
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

    [HttpGet("GetCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<List<TblCorporateCustomerEmployee>>>> GetCorporateEmployee()
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanViewStaff))
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

        var corporateCustomer = await UnitOfWork.CorporateEmployeeRepo.GetCorporateCustomerEmployees((Guid)CorporateProfile.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        return Ok(new ResponseDTO<List<TblCorporateCustomerEmployee>>(_data: corporateCustomer, success: true, _message: Message.Success));
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

    [HttpGet("GetPendingCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<List<TempCorporateEmployeeResponse>>>> GetPendingCorporateEmployee()
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

        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanViewStaff))
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

        var pendingEmployee = await UnitOfWork.TempCorporateEmployeeRepo.GetPendingCorporateEmployee(corporateCustomerDto.Id);
        if (!pendingEmployee.Any())
        {
          return Ok(new ResponseDTO<List<TempCorporateEmployeeResponse>>(_data: new List<TempCorporateEmployeeResponse>(), success: true, _message: Message.Success));
        }
        var mapResponse = Mapper.Map<List<TempCorporateEmployeeResponse>>(pendingEmployee);
        return Ok(new ResponseDTO<List<TempCorporateEmployeeResponse>>(_data: mapResponse, success: true, _message: Message.Success));
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

    [HttpPost("VerifyCorporateEmployeeBulkUpload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<TblCorporateCustomerEmployee>>> VerifyCorporateEmployeeBulkUpload([FromForm] EmployeeBulkUploadDto model)
    {
      try
      {
        var req = new Regex("^[0-9]*$");
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

        var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        if (tblCorporateCustomer == null)
        {
          return BadRequest("Invalid corporate customer id");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateStaff))
        {
          if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
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

        var dtb = _fileService.ReadEmployeeExcelFile(model.files);
        if (dtb.Count == 0)
        {
          return BadRequest("No Data found Reading Excel File");
        }

        var bulkTransactionItems = new List<VerifyBulkCorporateEmployeeResponseDto>();
        var bankList = await _apiService.GetBanks();
        if (bankList.ResponseCode != "00")
        {
          return BadRequest(bankList.ResponseMessage);
        }

        await Task.WhenAll(dtb.AsEnumerable().Select(async row =>
        {
          var errorMsg = "";
          if (string.IsNullOrEmpty(row.BankCode) || string.IsNullOrEmpty(row.BankCode?.Trim()))
          {
            errorMsg = "Bank code is empty;";
          }
          if (row.BankCode != null && row.BankCode.Length != 6)
          {
            errorMsg += "Invalid Bank code";
          }

          if (string.IsNullOrEmpty(row.AccountNumber) || string.IsNullOrEmpty(row.AccountNumber?.Trim()))
          {
            errorMsg += "Account number is empty;";
          }
          if (row.AccountNumber != null && row.AccountNumber.Length != 10)
          {
            errorMsg += "Credit account number is invalid ";
          }
          if (row.SalaryAmount <= 0)
          {
            errorMsg += "Credit amount is invalid;";
          }

          if (!req.IsMatch(row.AccountNumber))
          {
            errorMsg += "Credit account number is invalid ";
          }

          if (!req.IsMatch(row.BankCode))
          {
            errorMsg += "bank BankCode is invalid ";
          }

          if (bulkTransactionItems.Count != 0)
          {
            var duplicateAccountNumber = bulkTransactionItems?.Where(xtc => xtc.AccountNumber == row.AccountNumber && xtc.SalaryAmount == row.SalaryAmount).ToList();
            if (duplicateAccountNumber.Count > 0)
            {
              errorMsg += $"Account Number {row.AccountNumber} Already Exist";
            }
          }

          if (string.IsNullOrEmpty(errorMsg) || errorMsg.Contains($"this Account Number {row.AccountNumber} Already Exist"))
          {
            var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == row.BankCode);
            row.BankName = bank != null ? bank.InstitutionName : "Parallex Bank";
            var info = await _apiService.BankNameInquire(row.AccountNumber, row.BankCode);
            if (info.ResponseCode != "00")
            {
              errorMsg += $"{info.ResponseMessage} -> {info.ResponseCode}";
            }
            row.AccountName = info?.AccountName;
            row.BankName = bank?.InstitutionName ?? "Parallex Bank";
          }
          row.Error = errorMsg;
          bulkTransactionItems.Add(row);
        }));

        //check if list is greater than 0
        if (!bulkTransactionItems.Any())
        {
          return BadRequest("Error Reading Excel File. There must be at least one valid entry");
        }
        return Ok(new ListResponseDTO<VerifyBulkCorporateEmployeeResponseDto>(_data: bulkTransactionItems, success: true, _message: Message.Success));
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

    [HttpPost("InitiateCorporateEmployeeBulkUpload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseDTO<TblCorporateCustomerEmployee>>> InitiateCorporateEmployeeBulkUpload([FromForm] EmployeeBulkUploadDto model)
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


        var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
        if (tblCorporateCustomer == null)
        {
          return BadRequest("Invalid corporate customer id");
        }

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateStaff))
        {
          if (Enum.TryParse(tblCorporateCustomer.AuthorizationType.Replace(" ", "_"), out AuthorizationType authorizationType))
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

        var dtb = _fileService.ReadEmployeeExcelFile(model.files);
        if (dtb.Count == 0)
        {
          return BadRequest("No Data in Excel File To Read");
        }
        var bankList = await _apiService.GetBanks();

        if (bankList.ResponseCode != "00")
        {
          return BadRequest(bankList.ResponseMessage);
        }

        var employeeListItems = new List<TblCorporateCustomerEmployee>();
        var dbemployeeListItems = new List<TblCorporateCustomerEmployee>();
        await Task.WhenAll(dtb.AsEnumerable().Select(async row =>
        {
          var errorMsg = "";
          if (string.IsNullOrEmpty(row.BankCode) || string.IsNullOrEmpty(row.BankCode?.Trim()))
          {
            errorMsg = "Bank code is empty;";
          }
          if (row.BankCode != null && row.BankCode.Length != 6)
          {
            errorMsg += "Invalid Bank code";
          }

          if (string.IsNullOrEmpty(row.AccountNumber) || string.IsNullOrEmpty(row.AccountNumber?.Trim()))
          {
            errorMsg += "Account number is empty;";
          }
          if (row.AccountNumber != null && row.AccountNumber.Length != 10)
          {
            errorMsg += "Credit account number is invalid ";
          }
          if (row.SalaryAmount <= 0)
          {
            errorMsg += "Credit amount is invalid;";
          }

          if (employeeListItems.Count != 0)
          {
            var duplicateAccountNumber = employeeListItems?.Where(xtc => xtc.AccountNumber == row.AccountNumber && xtc.SalaryAmount == row.SalaryAmount).ToList();
            if (duplicateAccountNumber.Count > 0)
            {
              errorMsg += $"Account Number {row.AccountNumber} Already Exist";
            }
          }
          if (string.IsNullOrEmpty(errorMsg) || errorMsg.Contains($"this Account Number {row.AccountNumber} Already Exist"))
          {
            var employeeInfo = new TblCorporateCustomerEmployee
            {
              Id = Guid.NewGuid(),
              Sn = 0,
              CorporateCustomerId = tblCorporateCustomer.Id,
              FirstName = row?.FirstName,
              LastName = row?.LastName,
              StaffId = row?.StaffId,
              Department = row?.Department,
              AccountNumber = row?.AccountNumber,
              BankCode = row?.BankCode,
              SalaryAmount = row?.SalaryAmount,
              GradeLevel = row?.GradeLevel,
              Description = row?.Description,
              Status = (int)ProfileStatus.Active,
              DateCreated = DateTime.Now,
              InitiatorId = CorporateProfile?.Id,
              InitiatorUserName = CorporateProfile?.Username
            };
            var CheckDuplicateEmp = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(employeeInfo);
            if (CheckDuplicateEmp.IsDuplicate)
            {
              dbemployeeListItems.Add(employeeInfo);
            }
            else
            {
              var mapTempEmployee = _mapper.Map<TblTempCorporateCustomerEmployee>(employeeInfo);
              var checkTempResult = UnitOfWork.TempCorporateEmployeeRepo.CheckDuplicate(mapTempEmployee);
              if (checkTempResult.IsDuplicate)
              {
                var item = _mapper.Map<TblCorporateCustomerEmployee>(checkTempResult);
                dbemployeeListItems.Add(item);
              }
              else
              {
                var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == row.BankCode);
                row.BankName = bank != null ? bank.InstitutionName : "Parallex Bank";
                var info = await _apiService.BankNameInquire(row.AccountNumber, row.BankCode);
                if (info.ResponseCode != "00")
                {
                  errorMsg += $"{info.ResponseMessage} -> {info.ResponseCode}";
                }
                else
                {
                  employeeInfo.AccountName = info?.AccountName;
                  employeeInfo.BankCode = bank?.InstitutionName ?? "Parallex Bank";
                  employeeListItems.Add(employeeInfo);
                }
              }

            }
          }
        }));

        if (!employeeListItems.Any())
        {
          if (dbemployeeListItems.Any())
          {
            return Ok(new { Responsecode = "00", ResponseDescription = "Employee Uploaded Has duplicate in Entries", Data = dbemployeeListItems });
          }
          else
          {
            return Ok(new { Responsecode = "00", ResponseDescription = "No Employee Data Found on the excel file" });
          }

        }

        UnitOfWork.CorporateEmployeeRepo.AddRange(employeeListItems);
        UnitOfWork.Complete();
        return Ok(new { Responsecode = "00", ResponseDescription = "Employee Uploaded Successful" });
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

    [HttpPost("UpdateCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> UpdateCorporateEmployee(UpdateCorporateEmployeeRequest model)
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

        //check if corporate customer Id exist
        var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
        if (corporateCustomerDto == null)
        {
          return BadRequest("Invalid Corporate Customer ID");
        }

        Enum.TryParse(corporateCustomerDto.AuthorizationType.Replace(" ", "_"), out AuthorizationType _auth);

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanCreateStaff))
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

        var payload = new UpdateCorporateEmployeeDto
        {
          Id = Encryption.DecryptGuid(model.Id),
          CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
          FirstName = Encryption.DecryptStrings(model.FirstName),
          LastName = Encryption.DecryptStrings(model.LastName),
          StaffId = Encryption.DecryptStrings(model.StaffId),
          Department = Encryption.DecryptStrings(model.Department),
          AccountName = Encryption.DecryptStrings(model.AccountName),
          AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
          BankCode = Encryption.DecryptStrings(model.BankCode),
          SalaryAmount = Encryption.DecryptDecimals(model.SalaryAmount),
          Description = Encryption.DecryptStrings(model.Description),
          GradeLevel = Encryption.DecryptStrings(model.GradeLevel),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };
        if (ValidatePaload(out errormsg, null, payload))
        {
          return BadRequest(errormsg);
        }


        var previousEmployeeInfo = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(payload.Id);
        if (previousEmployeeInfo == null)
        {
          return BadRequest("Invalid Employee Id");
        }


        var mapEmployee = this.MapUpdateRequestDtoToCorporateEmployee(previousEmployeeInfo, payload);
        var checkResult = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(mapEmployee, true);
        if (checkResult.IsDuplicate)
        {
          return BadRequest(checkResult.Message);
        }

        var maptoTemp = this.MapToTempCorporateEmployee(mapEmployee);
        var checkTempResult = UnitOfWork.TempCorporateEmployeeRepo.CheckDuplicate(maptoTemp, true);
        if (checkTempResult.IsDuplicate)
        {
          return BadRequest(checkTempResult.Message);
        }

        this.AddAuditTrial(new AuditTrailDetail
        {
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Schedule Description: {payload.FirstName}, " +
            $"Last Name: {payload.LastName}, StaffId: {payload.StaffId}, Department:  {payload.Department}, " +
            $"AccountName: {payload.AccountName}, AccountNumber: {payload.AccountNumber}, BankCode: {payload.BankCode}, " +
            $"SalaryAmount: {payload.SalaryAmount}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Schedule Description: {payload.FirstName}, " +
            $"Last Name: {payload.LastName}, StaffId: {payload.StaffId}, Department:  {payload.Department}, " +
            $"AccountName: {payload.AccountName}, AccountNumber: {payload.AccountNumber}, BankCode: {payload.BankCode}, " +
            $"SalaryAmount: {payload.SalaryAmount}, Status: {nameof(ProfileStatus.Pending)}",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Update Corporate User. Action was carried out by a Bank user",
        });

        if (_auth == AuthorizationType.Single_Signatory)
        {
          mapEmployee.Status = (int)ProfileStatus.Active;
          UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(mapEmployee);
        }
        else
        {
          previousEmployeeInfo.Status = (int)ProfileStatus.Modified;
          UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(previousEmployeeInfo);
          UnitOfWork.TempCorporateEmployeeRepo.Add(maptoTemp);
        }
        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data: mapEmployee, success: true, _message: Message.Success));
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

    [HttpPost("DeactivateCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> DeactivateCorporateEmployee(AppAction model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanDeactivateStaff))
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

        if (string.IsNullOrEmpty(model.Reason))
        {
          return BadRequest("Reason for de-activating profile is required");
        }
        //get profile by id
        var payload = new SimpleAction
        {
          Id = Encryption.DecryptGuid(model.Id),
          Reason = Encryption.DecryptStrings(model.Reason),
          IPAddress = Encryption.DecryptStrings(model.IPAddress),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          HostName = Encryption.DecryptStrings(model.HostName),
          MACAddress = Encryption.DecryptStrings(model.MACAddress),
        };
        var entity = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (entity.Status == (int)ProfileStatus.Deactivated) return BadRequest("Employee is already de-activated");



        var status = (ProfileStatus)entity.Status;
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Deactivate).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
            $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department},AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
            $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Deactivated)}",
          PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
            $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department} AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
            $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Active)}",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Deactivated Corporate Empoloyee Initiated. Action was carried out by a Corporate user",
        });
        entity.Status = (int)ProfileStatus.Deactivated;
        UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data: entity, success: true, _message: Message.Success));
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

    [HttpPost("ReactivateCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>> ReactivateCorporateEmployee(AppAction model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanReactivateStaff))
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
        var entity = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (entity.Status == (int)ProfileStatus.Active) return BadRequest("Employee is already activated");

        var status = (ProfileStatus)entity.Status;
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
            $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department},AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
            $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Active)}",
          PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
            $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department} AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
            $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Deactivated)}",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Reactivated Corporate Empoloyee Initiated. Action was carried out by a Corporate user",
        });

        entity.Status = (int)ProfileStatus.Active;
        UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        UnitOfWork.Complete();
        return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data: entity, success: true, _message: Message.Success));
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

    [HttpGet("DownloadCorporateEmployeeTemplate")]
    public async Task<IActionResult> DownloadBankCodeTemplate()
    {
      if (!IsAuthenticated)
      {
        return StatusCode(401, "User is not authenticated");
      }
      //path to file
      //var folderName = Path.Combine("wwwroot", "bulkupload");
      var filePath = Path.Combine("wwwroot", "BulkUpload", "employeetemplate.xlsx");
      if (!System.IO.File.Exists(filePath))
        return NotFound();
      var memory = new MemoryStream();
      await using (var stream = new FileStream(filePath, FileMode.Open))
      {
        await stream.CopyToAsync(memory);
      }
      memory.Position = 0;
      return File(memory, Formater.GetContentType(filePath), "employee employeetemplate.xlsx");
    }

    [HttpPost("RequestCorporateEmployeeApproval")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<bool> RequestProfileApproval(SimpleActionDto model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanRequestStaffApproval))
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
          HostName = Encryption.DecryptStrings(model.HostName),
          ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
          MACAddress = Encryption.DecryptStrings(model.MACAddress)
        };
        var entity = UnitOfWork.TempCorporateEmployeeRepo.GetByIdAsync(payload.Id);
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
          return Ok(true);
        }
        return Ok(true);
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    [HttpPost("ApproveCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<bool> ApproveProfile(SimpleActionDto model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanApproveStaff))
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



        var entity = UnitOfWork.TempCorporateEmployeeRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (entity.InitiatorId != CorporateProfile.Id)
        {
          return BadRequest("This pending request was not done by you");
        }

        if (!ApprovedRequest(entity, payload, out string errorMessage))
        {
          return StatusCode(400, errorMessage);
        }

        if (entity.Action == nameof(TempTableAction.Create).Replace("_", " "))
        {
          return Ok(true);
        }
        return Ok(true);
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    [HttpPost("DeclineCorporateEmployee")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public ActionResult<bool> DeclineProfile(AppAction model)
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

        if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.CorporateUserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CanDeclineStaff))
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

        var entity = UnitOfWork.TempCorporateEmployeeRepo.GetByIdAsync(payload.Id);
        if (entity == null)
        {
          return BadRequest("Invalid Id");
        }

        if (!DeclineRequest(entity, payload, out string errorMessage))
        {
          return StatusCode(400, errorMessage);
        }
        return Ok(true);
      }
      catch (Exception ex)
      {
        _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
        return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
      }
    }

    private bool ApprovedRequest(TblTempCorporateCustomerEmployee requestInfo, SimpleAction payload, out string errorMessage)
    {

      var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)requestInfo.CorporateCustomerId);
      if (corporateCustomerDto == null)
      {
        errorMessage = "Invalid Corporate Customer ID";
        return false;
      }

      if (requestInfo.Action == nameof(TempTableAction.Create).Replace("_", " "))
      {
        var mapCorporateEmployee = Mapper.Map<TblCorporateCustomerEmployee>(requestInfo);
        var corporateEmployee = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(mapCorporateEmployee, false);
        if (corporateEmployee.IsDuplicate)
        {
          errorMessage = corporateEmployee.Message;
          return false;
        }
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Create).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
            $"Last Name: {requestInfo.LastName}, StaffId: {requestInfo.StaffId}, Department:  {requestInfo.Department}, " +
            $"AccountName: {requestInfo.AccountName}, AccountNumber: {requestInfo.AccountNumber}, BankCode: {requestInfo.BankCode}, " +
            $"SalaryAmount: {requestInfo.SalaryAmount}, Status: {nameof(ProfileStatus.Active)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Approved Newly Created Employee. Action was carried out by a Corporate use"
        });

        requestInfo.IsTreated = (int)ProfileStatus.Active;
        //requestInfo.ApproverId= CorporateProfile.Id;
        //requestInfo.ApproverUserName = UserName;
        requestInfo.DateApproved = DateTime.Now;
        mapCorporateEmployee.Sn = 0;
        mapCorporateEmployee.Status = (int)ProfileStatus.Active;
        UnitOfWork.TempCorporateEmployeeRepo.UpdateCorporateEmployee(requestInfo);
        UnitOfWork.CorporateEmployeeRepo.Add(mapCorporateEmployee);
        UnitOfWork.Complete();
        errorMessage = "";
        return true;
      }

      if (requestInfo.Action == nameof(TempTableAction.Update).Replace("_", " "))
      {
        var entity = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync((Guid)requestInfo.Id);
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Update).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {requestInfo.FirstName}, " +
            $"Last Name: {requestInfo.LastName}, StaffId: {requestInfo.StaffId}, Department:  {requestInfo.Department}, " +
            $"AccountName: {requestInfo.AccountName}, AccountNumber: {requestInfo.AccountNumber}, BankCode: {requestInfo.BankCode}, " +
            $"SalaryAmount: {requestInfo.SalaryAmount}, Status: {nameof(ProfileStatus.Pending)}",
          PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
            $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department}, " +
            $"AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
            $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Active)}",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Corporate User Create Corporate Employee. Action was carried out by a Corporate user"
        });

        entity.FirstName = requestInfo.FirstName;
        entity.LastName = requestInfo.LastName;
        entity.StaffId = requestInfo.StaffId;
        // entity.Frequency = requestInfo.Frequency;
        entity.Department = requestInfo.Department;
        entity.AccountNumber = requestInfo.AccountNumber;
        entity.BankCode = requestInfo.BankCode;
        entity.SalaryAmount = requestInfo.SalaryAmount;
        entity.GradeLevel = requestInfo.GradeLevel;
        entity.Description = requestInfo.Description;
        var userStatus = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(entity, true);
        if (userStatus.IsDuplicate)
        {
          errorMessage = userStatus.Message;
          return false;
        }
        requestInfo.IsTreated = (int)ProfileStatus.Active;
        entity.Status = (int)ProfileStatus.Active;
        //requestInfo.ApproverId = CorporateProfile.Id;
        //requestInfo.ApproverUserName = UserName;
        requestInfo.DateApproved = DateTime.Now;
        requestInfo.Reasons = payload.Reason;
        UnitOfWork.TempCorporateEmployeeRepo.UpdateCorporateEmployee(requestInfo);
        UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        //UnitOfWork.AuditTrialRepo.Add(auditTrail);
        UnitOfWork.Complete();
        errorMessage = userStatus.Message;
        return true;
      }
      errorMessage = "Unknow Request";
      return false;
    }
    private bool RequestApproval(TblTempCorporateCustomerEmployee entity, SimpleAction payload, out string errorMessage)
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
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
            $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department}, " +
            $"AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
            $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Active)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Request Approval for Newly Created Employee. Action was carried out by a Corporate use"
        });

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Pending;
        UnitOfWork.TempCorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        UnitOfWork.Complete();
        // notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
        errorMessage = "Request Approval Was Successful";
        return true;
      }

      if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
      {
        var employee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(entity.Id);
        if (employee == null)
        {
          errorMessage = "Invalid employee Id";
          return false;
        }
        if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
        {
          errorMessage = "employee wasn't Decline or modified initially";
          return false;
        }

        var status = (ProfileStatus)entity.Status;
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department}, " +
                $"AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
                $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Declined)}",
          PreviousFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {employee.FirstName}, " +
                $"Last Name: {employee.LastName}, StaffId: {employee.StaffId}, Department:  {employee.Department}, " +
                $"AccountName: {employee.AccountName}, AccountNumber: {employee.AccountNumber}, BankCode: {employee.BankCode}, " +
            $"SalaryAmount: {employee.SalaryAmount}, Status: {nameof(ProfileStatus.Active)}",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Request Approval for Employee Update. Action was carried out by a Corporate use"
        });

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Pending;
        employee.Status = (int)ProfileStatus.Pending;
        UnitOfWork.TempCorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(employee);
        UnitOfWork.Complete();
        //notify.NotifyBankAuthorizerForCorporate(entity.Action,entity,corporateCustomerDto,null,null,RoleName);
        errorMessage = "Request Approval Was Successful";
        return true;
      }

      errorMessage = "invalid Request";
      return false;
    }
    private bool DeclineRequest(TblTempCorporateCustomerEmployee entity, RequestCorporateProfileDto payload, out string errorMessage)
    {
      var initiatorProfile = UnitOfWork.CorporateProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);
      var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
      var pendingEmployee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync((Guid)entity.CorporateCustomerEmployeeId);
      if (corporateCustomerDto == null)
      {
        errorMessage = "Invalid Corporate Customer ID";
        return false;
      }
      if (pendingEmployee == null)
      {
        errorMessage = "Invalid Corporate Employee ID";
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
          errorMessage = "employee wasn't Decline or modified initially";
          return false;
        }
        var status = (ProfileStatus)entity.Status;
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Decline).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department}, " +
                $"AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
                $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Declined)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Decline Approval for Employee Creation. Action was carried out by a Corporate use"
        });

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Declined;
        entity.IsTreated = (int)ProfileStatus.Declined;
        entity.Reasons = payload.Reason;
        // entity.ApproverId = CorporateProfile.Id;
        //entity.ApproverUserName= UserName;
        entity.DateApproved = DateTime.Now;
        UnitOfWork.TempCorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        UnitOfWork.Complete();
        //notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
        errorMessage = "Decline Approval Was Successful";
        return true;
      }

      if (entity.Action == nameof(TempTableAction.Update).Replace("_", " "))
      {
        var employee = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync((Guid)entity.CorporateCustomerEmployeeId);
        if (employee == null)
        {
          errorMessage = "Invalid employee Id";
          return false;
        }

        if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified)
        {
          errorMessage = "employee wasn't Decline or modified initially";
          return false;
        }
        var status = (ProfileStatus)entity.Status;
        this.AddAuditTrial(new AuditTrailDetail
        {
          Action = nameof(AuditTrailAction.Decline).Replace("_", " "),
          NewFieldValue = $"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
                $"Last Name: {entity.LastName}, StaffId: {entity.StaffId}, Department:  {entity.Department}, " +
                $"AccountName: {entity.AccountName}, AccountNumber: {entity.AccountNumber}, BankCode: {entity.BankCode}, " +
                $"SalaryAmount: {entity.SalaryAmount}, Status: {nameof(ProfileStatus.Declined)}",
          PreviousFieldValue = "",
          Ipaddress = payload.IPAddress,
          Macaddress = payload.MACAddress,
          HostName = payload.HostName,
          ClientStaffIpaddress = payload.ClientStaffIPAddress,
          UserId = CorporateProfile.Id,
          UserName = UserName,
          Description = "Decline Approval for Employee Update. Action was carried out by a Corporate use"
        });

        //update status
        //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
        entity.Status = (int)ProfileStatus.Declined;
        employee.Status = (int)entity.PreviousStatus;
        entity.IsTreated = (int)ProfileStatus.Declined;
        entity.Reasons = payload.Reason;
        //entity.ApproverId = CorporateProfile.Id;
        //entity.ApproverUserName= UserName;
        entity.DateApproved = DateTime.Now;
        UnitOfWork.TempCorporateEmployeeRepo.UpdateCorporateEmployee(entity);
        UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(employee);
        UnitOfWork.Complete();
        //notify.NotifyCorporateMaker(initiatorProfile, entity.Action, notifyInfo, payload.Reason);
        errorMessage = "Decline Approval Was Successful";
        return true;
      }

      errorMessage = "invalid Request";
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
    private TblTempCorporateCustomerEmployee MapCreateRequestDtoToCorporateEmployee(CreateCorporateEmployeeDto payload)
    {
      var mapEmployee = Mapper.Map<TblTempCorporateCustomerEmployee>(payload);
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.IsTreated = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.InitiatorUserName = CorporateProfile.Username;
      mapEmployee.DateCreated = DateTime.Now;
      mapEmployee.Sn = 0;
      mapEmployee.Id = Guid.NewGuid();
      return mapEmployee;
    }
    private TblCorporateCustomerEmployee MapUpdateRequestDtoToCorporateEmployee(TblCorporateCustomerEmployee previous, UpdateCorporateEmployeeDto payload)
    {
      var mapEmployee = Mapper.Map<TblCorporateCustomerEmployee>(payload);
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.InitiatorUserName = CorporateProfile.Username;
      mapEmployee.DateCreated = DateTime.Now;
      return mapEmployee;
    }
    private TblTempCorporateCustomerEmployee MapToTempCorporateEmployee(TblCorporateCustomerEmployee employee)
    {
      var mapEmployee = Mapper.Map<TblTempCorporateCustomerEmployee>(employee);
      mapEmployee.CorporateCustomerEmployeeId = employee.Id;
      mapEmployee.Status = (int)ProfileStatus.Pending;
      mapEmployee.IsTreated = (int)ProfileStatus.Pending;
      mapEmployee.InitiatorId = CorporateProfile.Id;
      mapEmployee.DateCreated = DateTime.Now;
      mapEmployee.Sn = 0;
      mapEmployee.Id = Guid.NewGuid();
      return mapEmployee;
    }
    private static bool ValidatePaload(out string errorMessage, CreateCorporateEmployeeDto create = null, UpdateCorporateEmployeeDto update = null)
    {
      if (create != null)
      {
        if (string.IsNullOrEmpty(create.FirstName) || !new ReqEx().AlphabetOnly.IsMatch(create.FirstName))
        {
          errorMessage = "Validate First Name is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.LastName) || !new ReqEx().AlphabetOnly.IsMatch(create.LastName))
        {
          errorMessage = "Validate Last Name is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.StaffId))
        {
          errorMessage = "StaffId is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.Department))
        {
          errorMessage = "Department is require";
          return true;
        }
        if (string.IsNullOrEmpty(create.BankCode))
        {
          errorMessage = "Bank Code is require";
          return true;
        }
        if (create.SalaryAmount == null || create.SalaryAmount == 0)
        {
          errorMessage = "Salary Amount is require";
          return true;
        }

        if (string.IsNullOrEmpty(create.Description))
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

        errorMessage = "Ok";
        return false;

      }

      if (update != null)
      {

        if (string.IsNullOrEmpty(update.FirstName) || !new ReqEx().AlphabetOnly.IsMatch(update.FirstName))
        {
          errorMessage = "First Name is require";
          return true;
        }
        if (string.IsNullOrEmpty(update.LastName) || !new ReqEx().AlphabetOnly.IsMatch(update.LastName))
        {
          errorMessage = "Last Name is require";
          return true;
        }
        if (string.IsNullOrEmpty(update.StaffId))
        {
          errorMessage = "StaffId is require";
          return true;
        }
        if (string.IsNullOrEmpty(update.Department))
        {
          errorMessage = "Department is require";
          return true;
        }
        if (string.IsNullOrEmpty(update.BankCode))
        {
          errorMessage = "Bank Code is require";
          return true;
        }
        if (update.SalaryAmount == null || update.SalaryAmount == 0)
        {
          errorMessage = "Salary Amount is require";
          return true;
        }

        if (string.IsNullOrEmpty(update.Description))
        {
          errorMessage = "Description is require";
          return true;
        }

        if (string.IsNullOrEmpty(update.AccountNumber) || !new ReqEx().NumberOnly.IsMatch(update.AccountNumber))
        {
          errorMessage = "Validate Account Number is require";
          return true;
        }

        var isValid = Guid.TryParse(update.CorporateCustomerId.ToString(), out _);
        if (!isValid)
        {
          errorMessage = "invalid Corporate Customer Id";
          return true;
        }

        var isValidId = Guid.TryParse(update.Id.ToString(), out _);
        if (!isValidId)
        {
          errorMessage = "Id is required.";
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