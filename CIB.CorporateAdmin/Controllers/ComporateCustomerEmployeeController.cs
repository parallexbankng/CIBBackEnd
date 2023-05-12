
using CIB.Core.Common.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using CIB.Core.Common.Response;
using CIB.Core.Utils;
using System;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Common.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Dto;
using CIB.Core.Modules.CorporateSalarySchedule._CorporateEmployee.Validation;
using CIB.Core.Common;
using System.IO;
using System.Threading.Tasks;
using CIB.Core.Services.File;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Services.Api;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class ComporateCustomerEmployeeController : BaseAPIController
    {
        private readonly ILogger _logger;
        private readonly IFileService _fileService;
        private readonly IApiService _apiService;

        public ComporateCustomerEmployeeController(IApiService apiService,IFileService fileService,ILogger<ComporateCustomerEmployeeController> logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor) : base( unitOfWork, mapper,accessor)
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
                
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
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
                
                var validator = new CreateCorporateEmployeeValidation();
               
                var results =  validator.Validate(payload);
                
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }
               
                var corporateCustomerDto =  UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId.Value);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }
                var mapEmployee = this.MapCreateRequestDtoToCorporateEmployee(payload);
                var checkResult = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(mapEmployee);
                if(checkResult.IsDuplicate){
                    return BadRequest(checkResult.Message);
                }
                
                this.AddAuditTrial(new AuditTrailDetail  {
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
                    UserName =UserName,
                    Description = "Corporate User Create Corporate Employee. Action was carried out by a Corporate user"
                });
                UnitOfWork.CorporateEmployeeRepo.Add(mapEmployee);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data:mapEmployee,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("VerifyCorporateEmployeeBulkUpload")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<ResponseDTO<TblCorporateCustomerEmployee>>> VerifyCorporateEmployeeBulkUpload([FromForm]EmployeeBulkUploadDto model)
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
                
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }
                

                var dtb = _fileService.ReadExcelFile(model.files);
                if (dtb.Count == 0)
                {
                    return BadRequest("Error Reading Excel File");
                }
                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                var bulkTransactionItems = new List<VerifyBulkCorporateEmployeeResponseDto>();
                var bankList = await _apiService.GetBanks();
                if (bankList.ResponseCode != "00")
                {
                    return BadRequest(bankList.ResponseMessage);
                }

                Parallel.ForEach<VerifyBulkCorporateEmployeeResponseDto>((IEnumerable<VerifyBulkCorporateEmployeeResponseDto>)dtb.AsEnumerable(), async row => { 
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

                    if(bulkTransactionItems.Count != 0)
                    {  
                        var duplicateAccountNumber = bulkTransactionItems?.Where(xtc => xtc.AccountNumber == row.AccountNumber && xtc.SalaryAmount == row.SalaryAmount).ToList();
                        if(duplicateAccountNumber.Count > 0)
                        {
                            errorMsg += $"Account Number {row.AccountNumber} Already Exist";
                        }
                    }

                    if(string.IsNullOrEmpty(errorMsg) || errorMsg.Contains($"this Account Number {row.AccountNumber} Already Exist"))
                    {
                        var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == row.BankCode);
                        row.BankName = bank != null ? bank.InstitutionName : "";
                        var info = await _apiService.BankNameInquire(row.AccountNumber, row.BankCode);
                        if(info.ResponseCode != "00")
                        {
                            errorMsg += $"{info.ResponseMessage} -> {info.ResponseCode}";
                        }
                        row.AccountName = info.AccountName;
                    }
                    row.Error = errorMsg;
                    bulkTransactionItems.Add(row);
                });

                //check if list is greater than 0
                if (bulkTransactionItems.Count == 0)
                {
                    return BadRequest("Error Reading Excel File. There must be at least one valid entry");
                }
                return Ok(new ListResponseDTO<VerifyBulkCorporateEmployeeResponseDto>(_data:bulkTransactionItems,success:true, _message:Message.Success));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPost("InitiateCorporateEmployeeBulkUpload")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<ResponseDTO<TblCorporateCustomerEmployee>>> InitiateCorporateEmployeeBulkUpload([FromForm]EmployeeBulkUploadDto model)
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
                
                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.CreateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
                }
                
                var dtb = _fileService.ReadEmployeeExcelFile(model.files);
                if (dtb.Count == 0)
                {
                    return BadRequest("Error Reading Excel File");
                }
                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }
                var bankList = await _apiService.GetBanks();
                    
                if (bankList.ResponseCode != "00")
                {
                    return BadRequest(bankList.ResponseMessage);
                }

                var employeeListItems = new List<TblCorporateCustomerEmployee>();
                Parallel.ForEach<VerifyBulkCorporateEmployeeResponseDto>((IEnumerable<VerifyBulkCorporateEmployeeResponseDto>)dtb.AsEnumerable(), async row => { 
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

                    if(employeeListItems.Count != 0)
                    {  
                        var duplicateAccountNumber = employeeListItems?.Where(xtc => xtc.AccountNumber == row.AccountNumber && xtc.SalaryAmount == row.SalaryAmount).ToList();
                        if(duplicateAccountNumber.Count > 0)
                        {
                            errorMsg += $"Account Number {row.AccountNumber} Already Exist";
                        }
                    }

                    if(string.IsNullOrEmpty(errorMsg) || errorMsg.Contains($"this Account Number {row.AccountNumber} Already Exist"))
                    {
                        var bank = bankList.Banks.FirstOrDefault(ctx => ctx.InstitutionCode == row.BankCode);
                        row.BankName = bank != null ? bank.InstitutionName : "";
                        var info = await _apiService.BankNameInquire(row.AccountNumber, row.BankCode);
                        if(info.ResponseCode != "00")
                        {
                            errorMsg += $"{info.ResponseMessage} -> {info.ResponseCode}";
                        }
                        else
                        {
                            var employeeInfo = new TblCorporateCustomerEmployee
                            {
                                Id = Guid.NewGuid(),
                                Sn =0,
                                CorporateCustomerId=tblCorporateCustomer.Id,
                                FirstName= row.FirstName,
                                LastName= row.LastName,
                                StaffId= row.StaffId,
                                Department=row.Department,
                                AccountName=info.AccountName,
                                AccountNumber=info.AccountNumber,
                                BankCode=row.BankCode,
                                SalaryAmount=row.SalaryAmount,
                                GradeLevel=row.GradeLevel,
                                Description=row.Description,
                                Status= (int)ProfileStatus.Active,
                                DateCreated= DateTime.Now,
                                InitiatorId=CorporateProfile.Id,
                                InitiatorUserName= CorporateProfile.Username
                            };
                            employeeListItems.Add(employeeInfo);
                        }
                    }
                });

                UnitOfWork.CorporateEmployeeRepo.AddRange(employeeListItems);
                UnitOfWork.Complete();
               return Ok(new { Responsecode = "00", ResponseDescription = "Employee Uploaded Successful"});
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPut("UpdateCorporateEmployee")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ResponseDTO<TblCorporateCustomerEmployee>>UpdateCorporateEmployee(UpdateCorporateEmployeeRequest model)
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.UpdateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
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

                var validator = new UpdateCorporateEmployeeValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                //check if corporate customer Id exist
                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync(payload.CorporateCustomerId.Value);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }

                var previousEmployeeInfo = UnitOfWork.CorporateEmployeeRepo.GetByIdAsync(payload.Id);
                if(previousEmployeeInfo == null)
                {
                    return BadRequest("Invalid Corporate Employee Id");
                }
                var mapEmployee = this.MapUpdateRequestDtoToCorporateEmployee(previousEmployeeInfo,payload);
                var checkResult = UnitOfWork.CorporateEmployeeRepo.CheckDuplicate(mapEmployee,true);
                if(checkResult.IsDuplicate){
                    return BadRequest(checkResult.Message);
                }

                this.AddAuditTrial(new AuditTrailDetail  {
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, Schedule Description: {payload.FirstName}, " +
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
                    UserName =UserName,
                    Description = "Update Corporate User. Action was carried out by a Bank user",
                });
                UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(mapEmployee);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data:mapEmployee,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPut("DeactivateCorporateEmployee")]
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeactivateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
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

                if (entity.Status == (int) ProfileStatus.Deactivated) return BadRequest("Employee is already de-activated");

                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }

                var status = (ProfileStatus)entity.Status;
                this.AddAuditTrial(new AuditTrailDetail  {
                    Action = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
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
                
                entity.Status = (int)ProfileStatus.Active;
                UnitOfWork.CorporateEmployeeRepo.UpdateCorporateEmployee(entity);
                UnitOfWork.Complete();
                return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data:entity,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
        }

        [HttpPut("ReactivateCorporateEmployee")]
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

                if (string.IsNullOrEmpty(UserRoleId) || !UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.DeactivateCorporateUserProfile))
                {
                    return BadRequest("UnAuthorized Access");
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

                if (entity.Status == (int) ProfileStatus.Active) return BadRequest("Employee is already activated");
                var corporateCustomerDto = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
                if (corporateCustomerDto == null)
                {
                    return BadRequest("Invalid Corporate Customer ID");
                }
                var status = (ProfileStatus)entity.Status;
                this.AddAuditTrial(new AuditTrailDetail  {
                    Action = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
                    NewFieldValue =$"Company Name: {corporateCustomerDto.CompanyName}, Customer ID: {corporateCustomerDto.CustomerId}, First Name: {entity.FirstName}, " +
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
                return Ok(new ResponseDTO<TblCorporateCustomerEmployee>(_data:entity,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                }
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
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
        private TblCorporateCustomerEmployee MapCreateRequestDtoToCorporateEmployee(CreateCorporateEmployeeDto payload)
        {
            var mapEmployee = Mapper.Map<TblCorporateCustomerEmployee>(payload);
            mapEmployee.Status =(int) ProfileStatus.Pending;
            mapEmployee.InitiatorId = CorporateProfile.Id;
            mapEmployee.DateCreated = DateTime.Now;
            mapEmployee.Sn = 0;
            mapEmployee.Id = Guid.NewGuid();
            return mapEmployee;
        }
        
        private TblCorporateCustomerEmployee MapUpdateRequestDtoToCorporateEmployee(TblCorporateCustomerEmployee previous,UpdateCorporateEmployeeDto payload)
        {
            var mapEmployee = Mapper.Map<TblCorporateCustomerEmployee>(payload);
            mapEmployee.Status =(int) ProfileStatus.Pending;
            mapEmployee.InitiatorId = CorporateProfile.Id;
            mapEmployee.DateCreated = DateTime.Now;
            return mapEmployee;
        }
      
    }
}