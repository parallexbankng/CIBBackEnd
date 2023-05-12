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
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateCustomer.Mapper;
using CIB.Core.Services.Api;
using CIB.Core.Services.Api.Dto;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Services.Notification;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CIB.BankAdmin.Controllers
{
    [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class ManageAccountController : BaseAPIController
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ManageAccountController> _logger;
        private readonly IFileService _fileService;
        private readonly IEmailService _emailService;
         private readonly IConfiguration _config;
        public ManageAccountController(IConfiguration config,IEmailService emailService,IFileService fileService,ILogger<ManageAccountController> _logger,IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor,IApiService apiService) : base(mapper, unitOfWork, accessor)
        {
            this._apiService = apiService;
            this._logger = _logger;
            this._fileService = fileService;
            this._emailService = emailService;
            this._config = config;
        
        }
        [HttpGet("CustomerNameInquiry")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<ResponseDTO<CustomerDataResponseDto>>> CustomerNameInquiry(string accountNumber)
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

                if (string.IsNullOrEmpty(accountNumber))
                {
                    return BadRequest("Account number is required!!!");
                }
                //call name inquiry API
                var number = accountNumber.Trim();
                var accountInfo = await _apiService.GetCustomerDetailByAccountNumber(number);
                if(accountInfo.ResponseCode != "00"){
                    return BadRequest(accountInfo.ResponseDescription);
                }
                return Ok(new ResponseDTO<CustomerDataResponseDto>(_data:accountInfo,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                // _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                // return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));

                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        [HttpGet("GetAuthorizationTypes")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<ListResponseDTO<AuthorizationTypeModel>> GetAuthorizationTypes()
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

                var authorizationTypes = new List<AuthorizationTypeModel>();
                var enums = Enum.GetValues(typeof(AuthorizationType)).Cast<AuthorizationType>().ToList();
                foreach (var e in enums)
                {
                    authorizationTypes.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
                }
                return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:authorizationTypes,success:true, _message:Message.Success) );
            }
            catch (Exception ex)
            {
                 _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        [HttpPost("ValidateCorporateCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<bool> ValidateCorporateCustomerModel(ValidateCorporateCustomerRequestDto model)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new ValidateCorporateCustomerRequestDto
                {
                    CompanyName = Encryption.DecryptStrings(model.CompanyName),
                    Email = Encryption.DecryptStrings(model.Email),
                    CustomerId = Encryption.DecryptStrings(model.CustomerId),
                    DefaultAccountNumber = Encryption.DecryptStrings(model.DefaultAccountNumber),
                    DefaultAccountName = Encryption.DecryptStrings(model.DefaultAccountName),
                    AuthorizationType = Encryption.DecryptStrings(model.AuthorizationType),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };

                var validator = new ValidateCorporateCustomerValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new { }, _success: false, _validationResult: results.Errors));
                }
                var entity = UnitOfWork.CorporateCustomerRepo.GetCorporateCustomerByCustomerID(payload.CustomerId);
                //check if corporate Id exist
                if (entity != null)
                {
                    return BadRequest("Customer with the same customer Id already exist");
                }

                var tempEntity = UnitOfWork.TemCorporateCustomerRepo.GetCorporateCustomerByCustomerID(payload.CustomerId);
                if (tempEntity != null)
                {
                    return BadRequest("Customer with the same customer Id is awaiting approval");
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                 _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
       
        [HttpPut("ValidateAccountLimit")]
        [ProducesResponseType(StatusCodes.Status201Created)]
       
        public ActionResult<bool> ValidateAccountLimitModel(AccountLimitRequestDto model)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new AccountLimitRequest
                {
                    MaxAccountLimit = Encryption.DecryptDecimals(model.MaxAccountLimit),
                    MinAccountLimit = Encryption.DecryptDecimals(model.MinAccountLimit),
                    SingleTransDailyLimit = Encryption.DecryptDecimals(model.SingleTransDailyLimit),
                    BulkTransDailyLimit = Encryption.DecryptDecimals(model.BulkTransDailyLimit),
                    AuthorizationType = Encryption.DecryptStrings(model.AuthorizationType),
                    IsApprovalByLimit = Encryption.DecryptBooleans(model.IsApprovalByLimit),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };
                if (payload.AuthorizationType != nameof(AuthorizationType.Multiple_Signatory))
                {
                    if (payload.MinAccountLimit < 0)
                    {
                        return BadRequest("Minimum account limit is invalid");
                    }

                    if (payload.MaxAccountLimit == 0)
                    {
                        return BadRequest("Maximum account limit must be greater than 0");
                    }

                    if (payload.MinAccountLimit > payload.MaxAccountLimit)
                    {
                        return BadRequest("Minimum account limit must not be greater than Maximum account limit");
                    }
                }
                if(payload.MaxAccountLimit > payload.SingleTransDailyLimit)
                {
                    return BadRequest("Maximum Limit per Transaction can not be greater than accumulative single daily Limit");
                }

                if(payload.MinAccountLimit < 1)
                {
                    return BadRequest("MinAccountLimit can not be less than 1");
                }

                if(payload.MinAccountLimit > payload.MaxAccountLimit)
                {
                    return BadRequest("MinAccountLimit can not be greater MaxAccountLimit");
                }

                if(payload.MaxAccountLimit > payload.BulkTransDailyLimit)
                {
                    return BadRequest("Maximum Limit per Transaction can not be greater than accumulative bulk daily Limit");
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                 _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("OnboardCorporateCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<bool> OnboardCorporateCustomer(OnboardCorporateCustomerRequestDto model)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new OnboardCorporateCustomer
                {
                    CompanyName = Encryption.DecryptStrings(model.CompanyName),
                    Email = Encryption.DecryptStrings(model.Email),
                    CustomerId = Encryption.DecryptStrings(model.CustomerId),
                    DefaultAccountNumber = Encryption.DecryptStrings(model.DefaultAccountNumber),
                    DefaultAccountName = Encryption.DecryptStrings(model.DefaultAccountName),
                    AuthorizationType = Encryption.DecryptStrings(model.AuthorizationType),
                    CorporateCustomerId = Encryption.DecryptGuid(model.CorporateCustomerId),
                    CorporateRoleId = Encryption.DecryptGuid(model.CorporateRoleId),
                    Username = Encryption.DecryptStrings(model.Username),
                    CorporateEmail = Encryption.DecryptStrings(model.CorporateEmail),
                    FirstName = Encryption.DecryptStrings(model.FirstName),
                    MiddleName = Encryption.DecryptStrings(model.MiddleName),
                    ApprovalLimit = Encryption.DecryptDecimals(model.ApprovalLimit),
                    MinAccountLimit = Encryption.DecryptDecimals(model.MinAccountLimit),
                    MaxAccountLimit = Encryption.DecryptDecimals(model.MaxAccountLimit),
                    SingleTransDailyLimit = Encryption.DecryptDecimals(model.SingleTransDailyLimit),
                    BulkTransDailyLimit = Encryption.DecryptDecimals(model.BulkTransDailyLimit),
                    LastName = Encryption.DecryptStrings(model.LastName),
                    PhoneNumber = Encryption.DecryptStrings(model.PhoneNumber),
                    IsApprovalByLimit = Encryption.DecryptBooleans(model.IsApprovalByLimit),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };
                
                var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetCorporateCustomerByCustomerID(payload.CustomerId);
                if (corporateCustomer != null)
                {
                    return BadRequest("Customer with the same customer Id already exist");
                }

                if (payload.AuthorizationType == nameof(AuthorizationType.Multiple_Signatory))
                {
                    if (payload.ApprovalLimit <= 0)
                    {
                        return BadRequest("Approval Limit is required");
                    }
                }
                else
                {
                   if (payload.ApprovalLimit <= 0)
                    {
                        return BadRequest("Approval Limit is required");
                    }
                    if (payload.MinAccountLimit < 0)
                    {
                        return BadRequest("Minimum account limit is invalid");
                    }

                    if (payload.MaxAccountLimit == 0)
                    {
                        return BadRequest("Maximum account limit must be greater than 0");
                    }

                    if (payload.MinAccountLimit > payload.MaxAccountLimit)
                    {
                        return BadRequest("Minimum account limit must not be greater than Maximum account limit");
                    }
                }
                if(payload.MaxAccountLimit > payload.SingleTransDailyLimit)
                {
                    return BadRequest("Maximum Limit per Transaction can not be greater than accumulative single daily Limit");
                }

                if(payload.MinAccountLimit < 1)
                {
                    return BadRequest("MinAccountLimit can not be less than 1");
                }

                if(payload.MaxAccountLimit > payload.BulkTransDailyLimit)
                {
                    return BadRequest("Maximum Limit per Transaction can not be greater than accumulative bulk daily Limit");
                }
                 if(payload.MinAccountLimit > payload.MaxAccountLimit)
                {
                    return BadRequest("MinAccountLimit can not be greater MaxAccountLimit");
                }

                var validator = new OnboardCorporateCustomerValidation();
                var results =  validator.Validate(payload);
                if (!results.IsValid)
                {
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
                }
                var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync((Guid)payload.CorporateRoleId);
                if (tblRole == null)
                {
                    BadRequest("Invalid role id");
                }

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Onboard).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue =   $"Company Name: {payload.CompanyName}, Customer ID: {payload.CustomerId}, " +
                    $"Authorization Type: {payload.AuthorizationType.Replace("_", " ")}, Default Account Name: {payload.DefaultAccountName}, " +
                    $"Default Account Number: {payload.DefaultAccountNumber}, Email: {payload.Email1}, Status: {nameof(ProfileStatus.Pending)}, " +
                    $"Maximum Account Limit: {payload.MaxAccountLimit}, Minimum Account Limit: {payload.MinAccountLimit}, Daily Limit Account Limit: {payload.SingleTransDailyLimit} Bulk Daily Limit: {payload.BulkTransDailyLimit} " +
                    $"Admin First Name: {payload.FirstName}, Admin Last Name: {payload.LastName}, Admin Username: {payload.Username}, Admin Email Address:  {payload.Email}, " +
                    $"Admin Middle Name: {payload.MiddleName}, Admin Phone Number: {payload.PhoneNumber}, Admin Role: {tblRole?.RoleName}, Admin Approval Limit {payload.ApprovalLimit}",
                    PreviousFieldValue ="",
                    TransactionId = "",
                    UserId = BankProfile.Id,
                    Username = UserName,
                    Description = " Onboarded Corporate Customer By Bank Admin",
                    TimeStamp = DateTime.Now
                };

                //create new corporate customer info
                var mapCustomer = Mapper.Map<TblCorporateCustomer>(payload);
                mapCustomer.Email1 = payload.Email;
                mapCustomer.Phone1 = payload.PhoneNumber;
                var customerStatus = UnitOfWork.CorporateCustomerRepo.CheckDuplicate(mapCustomer);

                if(customerStatus.IsDuplicate != "02")
                {
                    _logger.LogError("DUPLICATE ERROR {0}, {1}",Formater.JsonType(customerStatus.IsDuplicate), Formater.JsonType(customerStatus.Message));
                    return BadRequest(new ErrorResponse(responsecode:ResponseCode.DUPLICATE_VALUE, responseDescription: customerStatus.Message, responseStatus:false));
                }
                
                var middleName = payload.MiddleName == null ? "" : payload.MiddleName.Trim().ToLower();
                var temCustomer = new TblTempCorporateCustomer
                {
                    Id = Guid.NewGuid(),
                    AuthorizationType = payload.AuthorizationType,
                    CompanyName = payload.CompanyName,
                    CorporateRoleId = payload.CorporateRoleId,
                    CustomerId = payload.CustomerId,
                    DefaultAccountName = payload.DefaultAccountName,
                    DefaultAccountNumber = payload.DefaultAccountNumber,
                    Email1 = payload.Email,
                    CorporateEmail = payload.CorporateEmail,
                    Phone1 = payload.PhoneNumber,
                    Status = (int) ProfileStatus.Modified,
                    RegStage =(int) ProfileStatus.Pending,
                    FirstName = payload.FirstName,
                    LastName = payload.LastName,
                    MiddleName = payload.MiddleName,
                    MaxAccountLimit = payload.MaxAccountLimit,
                    BulkTransDailyLimit = payload.BulkTransDailyLimit,
                    MinAccountLimit = payload.MinAccountLimit,
                    SingleTransDailyLimit = payload.SingleTransDailyLimit,
                    UserName = payload.Username,
                    IsTreated = (int) ProfileStatus.Pending,
                    InitiatorId = BankProfile.Id,
                    InitiatorUsername = UserName,
                    DateRequested = DateTime.Now,
                    FullName = payload.FirstName.Trim().ToLower() +" "+ middleName + " " +payload.LastName.Trim().ToLower(),
                    Action = nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " ")
                };

                var customerStatu= UnitOfWork.TemCorporateCustomerRepo.CheckDuplicate(temCustomer);

                if(customerStatu.IsDuplicate != "02")
                {
                    _logger.LogError("DUPLICATE ERROR {0}, {1}",Formater.JsonType(customerStatus.IsDuplicate), Formater.JsonType(customerStatus.Message));
                    return BadRequest(new ErrorResponse(responsecode:ResponseCode.DUPLICATE_VALUE, responseDescription: customerStatu.Message, responseStatus:false));
                }
                UnitOfWork.TemCorporateCustomerRepo.Add(temCustomer);
                UnitOfWork.Complete();
                return Ok(true);
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
   
        [HttpPost("BulkOnboardCorporateCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<bool>> BulkOnboardCorporateCustomer([FromForm] BulkOnboardCorporateCustomerRequestDto model)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var dtb = _fileService.ReadCorporateCustomerExcelFile(model.files);
                if (dtb.Count == 0)
                {
                    return BadRequest("Error Reading Excel File");
                }

                var payload = new OnboardCorporateCustomer
                {
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };

                var corporateCustomer = new List<TblCorporateCustomer>();
                var corporateProfiles = new List<TblCorporateProfile>();
                var audit = new List<TblAuditTrail>();
                var duplicate = new List<BulkCustomerOnboading>();
                var creditials = new List<BulkSendCreditial>();
                Parallel.ForEach(dtb.AsEnumerable(), async row => 
                {
                    var validateCustomerId = await _apiService.RelatedCustomerAccountDetails(row.CustomerId);
                    if(validateCustomerId.RespondCode != "00")
                    {
                        row.Error = validateCustomerId.RespondMessage;
                        duplicate.Add(row);
                    }
                    else
                    {
                        var checkDuplicate = _unitOfWork.CorporateCustomerRepo.GetCorporateCustomerByCustomerID(row.CustomerId);
                        if(checkDuplicate == null)
                        {
                            var accountInfo = await _apiService.GetCustomerDetailByAccountNumber(row.DefaultAccountName);
                            if(accountInfo.ResponseCode != "00"){
                                BadRequest(accountInfo.ResponseDescription);
                            }
                            var task = Task.Run(() => 
                            {
                                var corporateCustomer = new TblCorporateCustomer
                            {
                                Id = Guid.NewGuid(),
                                Sn= 0,
                                AuthorizationType = row.AuthorizationType,
                                CompanyName = row.CompanyName,
                                CompanyAddress = accountInfo.Address,
                                CustomerId = row.CustomerId,
                                DefaultAccountName = accountInfo.AccountName,
                                DefaultAccountNumber = row.DefaultAccountNumber,
                                Email1 = row.Email,
                                CorporateEmail = accountInfo.Email,
                                Phone1 = accountInfo.MobileNo,
                                Status = (int) ProfileStatus.Modified,
                                MaxAccountLimit = row.MaxAccountLimit,
                                BulkTransDailyLimit = row.BulkTransDailyLimit,
                                MinAccountLimit = row.MinAccountLimit,
                                SingleTransDailyLimit = row.SingleTransDailyLimit,
                                DateAdded = DateTime.Now,
                            };

                                var fullName = row.MiddleName == null ? row.FirstName.Trim().ToLower() + " "+ row.LastName.Trim().ToLower() : row.FirstName.Trim().ToLower() + " " +row.MiddleName.Trim().ToLower() +" "+ row.LastName.Trim().ToLower();
                                var password =  Encryption.EncriptPassword(PasswordValidator.GeneratePassword());
                                var corporateProfile = new TblCorporateProfile
                                {
                                    Id = Guid.NewGuid(),
                                    Sn=0,
                                    CorporateCustomerId = corporateCustomer.Id,
                                    Username = row.Username,
                                    Email = row.Email,
                                    MiddleName =row.MiddleName,
                                    LastName = row.LastName,
                                    FirstName = row.FirstName,
                                    Phone1 = row.PhoneNumber,
                                    Password = password,
                                    ApprovalLimit = row.ApprovalLimit,
                                    Status = (int) ProfileStatus.Modified,
                                    RegStage = (int) ProfileStatus.Pending,
                                    FullName = fullName,
                                
                                };
                                var auditTrail = new TblAuditTrail
                                {
                                    Id = Guid.NewGuid(),
                                    ActionCarriedOut = nameof(AuditTrailAction.Bulk_Customer_Onboard).Replace("_", " "),
                                    Ipaddress = payload.IPAddress,
                                    Macaddress = payload.MACAddress,
                                    HostName = payload.HostName,
                                    NewFieldValue =   $"Company Name: {row.CompanyName}, Customer ID: {row.CustomerId}, " +
                                    $"Authorization Type: {row.AuthorizationType.Replace("_", " ")}, Default Account Name: {row.DefaultAccountName}, " +
                                    $"Default Account Number: {row.DefaultAccountNumber}, Email: {row.Email1}, Status: {nameof(ProfileStatus.Pending)}, " +
                                    $"Maximum Account Limit: {row.MaxAccountLimit}, Minimum Account Limit: {row.MinAccountLimit}, Daily Limit Account Limit: {row.SingleTransDailyLimit} Bulk Daily Limit: {row.BulkTransDailyLimit} " +
                                    $"Admin First Name: {row.FirstName}, Admin Last Name: {row.LastName}, Admin Username: {row.Username}, Admin Email Address:  {row.Email}, " +
                                    $"Admin Middle Name: {row.MiddleName}, Admin Phone Number: {row.PhoneNumber}, Admin Approval Limit {row.ApprovalLimit}",
                                    PreviousFieldValue ="",
                                    TransactionId = "",
                                    UserId = BankProfile.Id,
                                    Username = UserName,
                                    Description = " Bulk Corporate Customer Onboard Corporate Customer By Bank Admin",
                                    TimeStamp = DateTime.Now
                                };
                                return  (corporateCustomer,corporateProfile,auditTrail);
                            });
                            corporateCustomer.Add(task.Result.corporateCustomer);
                            corporateProfiles.Add(task.Result.corporateProfile);
                            audit.Add(task.Result.auditTrail);
                        }
                        else
                        {
                            row.Error = "Corporate Customer Id Already Exisit";
                            duplicate.Add(row);
                        }
                    }
                
                });
                UnitOfWork.AuditTrialRepo.AddRange(audit);
                UnitOfWork.CorporateCustomerRepo.AddRange(corporateCustomer);
                UnitOfWork.CorporateProfileRepo.AddRange(corporateProfiles);
                UnitOfWork.Complete();
                foreach(var profile in corporateProfiles )
                {
                    var authUrl = _config.GetValue<string>("authUrl:coporate");
                    var comany = corporateCustomer.Where(xtc => xtc.Id == profile.CorporateCustomerId).FirstOrDefault();
                    var password = Encryption.DecriptPassword(profile.Password);
                    await _emailService.SendEmail(EmailTemplate.LoginCredentialMail(profile.Email, profile.FullName, profile.Username, password, comany.CustomerId,authUrl));
                }
                if(duplicate.Any())
                {
                    return Ok(duplicate);
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
   
        [HttpPut("ChangeCorporateCustomerSignatory")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<bool> ChangeCorporateCustomerSignatory(AccountLimitRequestDto model)
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

                if (!UnitOfWork.UserRoleAccessRepo.AccessesExist(UserRoleId, Permission.OnboardCorporateCustomer))
                {
                    return BadRequest("UnAuthorized Access");
                }

                var payload = new AccountLimitRequest
                {
                    MaxAccountLimit = Encryption.DecryptDecimals(model.MaxAccountLimit),
                    MinAccountLimit = Encryption.DecryptDecimals(model.MinAccountLimit),
                    SingleTransDailyLimit = Encryption.DecryptDecimals(model.SingleTransDailyLimit),
                    BulkTransDailyLimit = Encryption.DecryptDecimals(model.BulkTransDailyLimit),
                    AuthorizationType = Encryption.DecryptStrings(model.AuthorizationType),
                    IsApprovalByLimit = Encryption.DecryptBooleans(model.IsApprovalByLimit),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };
                if (payload.AuthorizationType != nameof(AuthorizationType.Multiple_Signatory))
                {
                    if (payload.MinAccountLimit < 0)
                    {
                        return BadRequest("Minimum account limit is invalid");
                    }

                    if (payload.MaxAccountLimit == 0)
                    {
                        return BadRequest("Maximum account limit must be greater than 0");
                    }

                    if (payload.MinAccountLimit > payload.MaxAccountLimit)
                    {
                        return BadRequest("Minimum account limit must not be greater than Maximum account limit");
                    }
                }
                if(payload.MaxAccountLimit > payload.SingleTransDailyLimit)
                {
                    return BadRequest("Maximum Limit per Transaction can not be greater than accumulative single daily Limit");
                }

                if(payload.MinAccountLimit < 1)
                {
                    return BadRequest("MinAccountLimit can not be less than 1");
                }

                if(payload.MinAccountLimit > payload.MaxAccountLimit)
                {
                    return BadRequest("MinAccountLimit can not be greater MaxAccountLimit");
                }

                if(payload.MaxAccountLimit > payload.BulkTransDailyLimit)
                {
                    return BadRequest("Maximum Limit per Transaction can not be greater than accumulative bulk daily Limit");
                }
                return Ok(true);
            }
            catch (Exception ex)
            {
                 _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
       

   
   }
}