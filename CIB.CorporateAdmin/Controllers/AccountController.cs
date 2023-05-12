using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.Authentication.Dto;
using CIB.Core.Modules.Authentication.Validation;
using CIB.Core.Modules.BankAdminProfile.Dto;
using CIB.Core.Modules.SecurityQuestion.Dto;
using CIB.Core.Services._2FA;
using CIB.Core.Services.Api;
using CIB.Core.Services.Email;
using CIB.Core.Templates;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class AccountController : BaseAPIController
    {
        protected readonly IConfiguration _config;
        protected readonly IEmailService _emailService;
        protected readonly IApiService _apiService;
        protected readonly IToken2faService _2faService;
        private readonly ILogger<AccountController> _logger;
        public AccountController(ILogger<AccountController> logger,IConfiguration config, IUnitOfWork unitOfWork,IMapper mapper,IEmailService emailService,IApiService apiService,IHttpContextAccessor accessor,IToken2faService _2faService):base(unitOfWork,mapper,accessor)
        {
            _config = config;
            _emailService = emailService;
            _apiService = apiService;
            this._2faService = _2faService;
            this._logger = logger;
            //_mailSettings = mailSettings.Value;
        }

        // <summary>
        // Bank User Login 
        // </summary>
        // <param name="Username">Username</param>
        // <param name="Password">Password</param>
        // <param name="Token">Token</param>
        // <returns>tokem  </returns>
        // <response code="200">Returns a boolean value indicating where the reset mail has been sent successful</response>
        // <response code="400">If the item is null </response> 
        [AllowAnonymous]
        [HttpPost("Login")]
        public ActionResult Login([FromBody] CustomerLoginParam login)
        {
            try
            {
                var payLoad = new CustomerLoginParam
                {
                    Username = Encryption.DecryptStrings(login.Username),
                    Password = Encryption.DecryptStrings(login.Password),
                    CustomerID = Encryption.DecryptStrings(login.CustomerID),
                    OTP = Encryption.DecryptStrings(login.OTP),
                    ClientStaffIPAddress = Encryption.DecryptStrings(login.ClientStaffIPAddress)
                };
                // VALIDATION
                var validator = new CorporateLoginValidation();
                var results = validator.Validate(payLoad);
                if (!results.IsValid)
                {
                    _logger.LogInformation("Invalid Request Data {0}",JsonConvert.SerializeObject(results));
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                // var userName = $"{payLoad.Username}{payLoad.CustomerID}";
                // var validOTP = await _2faService.TokenAuth(userName, payLoad.OTP);
                // if(validOTP.ResponseCode != "00"){
                //     return BadRequest($"2FA Service Error {validOTP.ResponseMessage}");
                // }

                var mycorpr = UnitOfWork.CustomerAuthenticationRepo.VerifyCorporateProfileByCustomerId(payLoad.CustomerID);
                if (mycorpr == null)
                {
                    _logger.LogInformation("Invalid Request Data {0}","Invalid CustomerId");
                    return BadRequest(new LoginResponsedata { Responsecode = "121", ResponseDescription = "Invalid CustomerId", UserpasswordChanged = 0, CustomerIdentity = "" });
                }
                if (mycorpr.Status == (int)ProfileStatus.Deactivated)
                {
                    _logger.LogInformation("Invalid Request Data {0}","Invalid CustomerId");
                    return BadRequest(new LoginResponsedata { Responsecode = "121", ResponseDescription = "Your Organization's profile has been temporarily deactivated", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                var cusauth = UnitOfWork.CustomerAuthenticationRepo.VerifyCorporateProfileUserName(payLoad, mycorpr.Id);
                if (cusauth == null)
                {
                    _logger.LogInformation("Invalid Request Data {0}","Invalid login attempt");
                    return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Invalid UserName", UserpasswordChanged = 0, CustomerIdentity = "" });
                }
                var corpcust = UnitOfWork.CorporateCustomerRepo.Find(ctx => ctx.Id == cusauth.CorporateCustomerId);

                //  generobj.WriteError("6a GET HERE AT  " + DateTime.Now.ToString());
                if (cusauth.NoOfWrongAttempts == 3 || cusauth.NoOfWrongAttempts > 3)
                {
                    cusauth.ReasonsForDeactivation = "Multiple incorrect login attempt";
                    cusauth.Status = -1;
                    var auditt = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. Multiple incorrect login",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditt);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(cusauth);
                    UnitOfWork.Complete();
                    _logger.LogInformation("you are not profile on this application, Please Contact Bank Admin {0}", JsonConvert.SerializeObject(payLoad));
                    return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                if (corpcust.CustomerId != payLoad.CustomerID)
                {
                    int wrontloginatempt = cusauth.NoOfWrongAttempts ?? 0;
                    cusauth.NoOfWrongAttempts = wrontloginatempt + 1;
                    cusauth.LastLoginAttempt = DateTime.Now;
                    var auditt = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. Invalid Customer Id",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditt);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(cusauth);
                    UnitOfWork.Complete();
                    return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Invalid login attempt", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                // generobj.WriteError("7a GET HERE AT  " + DateTime.Now.ToString());
                string emppass = Encryption.OpenSSLDecrypt(cusauth.Password, Encryption.GetEncrptionKey());
                if (emppass != payLoad.Password)
                {
                    int wrontloginatempt = cusauth.NoOfWrongAttempts ?? 0;
                    cusauth.NoOfWrongAttempts = wrontloginatempt + 1;
                    cusauth.LastLoginAttempt = DateTime.Now;
                    var auditt = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. Invalid Password",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditt);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(cusauth);
                    UnitOfWork.Complete();
                    payLoad.Password = "";
                    _logger.LogInformation("Invalid login attempt. Invalid Password {0}", JsonConvert.SerializeObject(payLoad));
                    return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Invalid User Name Or Password", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                // generobj.WriteError("8a GET HERE AT  " + DateTime.Now.ToString());
                if (cusauth.Status == (int) ProfileStatus.Deactivated)
                {
                    var auditt = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. profile has been deactivated",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditt);
                    payLoad.Password = "";
                    _logger.LogInformation("Sorry, your profile has been deactivated, please contact our support team {0}", JsonConvert.SerializeObject(payLoad));
                    return BadRequest(new LoginResponsedata { Responsecode = "13", ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                //check if it's first time login
                if (cusauth.Passwordchanged != 1)
                {
                    var audittrail = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. First time login detected. Password needs to be changed",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(audittrail);
                    UnitOfWork.Complete();
                    payLoad.Password = "";
                    _logger.LogInformation("Sorry, first time login has been detected. You need to change your default password. {0}", JsonConvert.SerializeObject(payLoad));
                    return Ok(new LoginResponsedata { Responsecode = "14", ResponseDescription = "Sorry, first time login has been detected. You need to change your default password.", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                //check if password reset was initiated
                if (cusauth.ResetInitiated == 1)
                {
                    var auditt = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. Password reset was triggered. Password needs to be changed",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditt);
                    payLoad.Password = "";
                    _logger.LogInformation("Sorry, we noticed a password reset was initiated.  You need to change your password before you can login. {0}", JsonConvert.SerializeObject(payLoad));
                    return Ok(new LoginResponsedata { Responsecode = "15", ResponseDescription = "Sorry, we noticed a password reset was initiated.  You need to change your password before you can login.", UserpasswordChanged = 0, CustomerIdentity = "" });
                }

                //check if security question has been set
                if (cusauth.RegStage == 0 || cusauth.RegStage < 2)
                {
                    var auditt = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. ISecurity question not yet set",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditt);
                    payLoad.Password = "";
                    _logger.LogInformation("Sorry, we noticed you are yet to setup your security question.  You need to setup a security question and answer before you can login. {0}", JsonConvert.SerializeObject(payLoad));
                    return Ok(new LoginResponsedata { Responsecode = "16", ResponseDescription = "Sorry, we noticed you are yet to setup your security question.  You need to setup a security question and answer before you can login.", UserpasswordChanged = cusauth.Passwordchanged ?? 0, CustomerIdentity = "" });
                }

                //check if password has expired (30 days)
                if (cusauth.PasswordExpiryDate != null && cusauth.PasswordExpiryDate.Value < DateTime.Now.AddDays(-30))
                {
                    var auditTrail = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. Password expired",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditTrail);
                    UnitOfWork.Complete();
                    payLoad.Password = "";
                    _logger.LogInformation("Sorry, we noticed your password has expired. You need to set a new password before you can login. {0}", JsonConvert.SerializeObject(payLoad));
                    return BadRequest(new LoginResponsedata { Responsecode = "17", ResponseDescription = "Sorry, we noticed your password has expired. You need to set a new password before you can login.", UserpasswordChanged = cusauth.Passwordchanged ?? 0, CustomerIdentity = "" });
                }

                //check if last activity (90 days)
                if (cusauth.LastActivity != null && cusauth.LastActivity.Value < DateTime.Now.AddDays(-90))
                {
                    cusauth.ReasonsForDeactivation = "Inactive for 90 days";
                    cusauth.Status = (int)ProfileStatus.Deactivated;
                    var auditTrail = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Login),
                        Ipaddress = payLoad.ClientStaffIPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                        $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = cusauth.Id,
                        Username = cusauth.Username,
                        Description = "Login Attempt Failure. User disabled due to inactivity for about 90 days",
                        TimeStamp = DateTime.Now
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditTrail);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(cusauth);
                    UnitOfWork.Complete();
                    payLoad.Password = "";
                    _logger.LogInformation("Sorry, we noticed your account has been inactive for about 90 days and has been suspended. Please contact your bank admin. {0}", JsonConvert.SerializeObject(payLoad));
                    return BadRequest(new LoginResponsedata { Responsecode = "18", ResponseDescription = "Sorry, we noticed your account has been inactive for about 90 days and has been suspended. Please contact your bank admin.", UserpasswordChanged = cusauth.Passwordchanged ?? 0, CustomerIdentity = "" });
                }

                var tokenBlack = UnitOfWork.TokenBlackCorporateRepo.GetBlackTokenById(cusauth.Id);
                foreach (var mykn in tokenBlack)
                {
                    mykn.IsBlack = 1;
                    UnitOfWork.TokenBlackCorporateRepo.UpdateTokenBlackCorporate(mykn);
                }

                string lastlogindate;

                if (cusauth.LastLogin == null)
                {
                    lastlogindate = "";
                }
                else
                {
                    DateTime LastLoginDate = cusauth.LastLogin ?? DateTime.Now;
                    string TimeIn12Format = LastLoginDate.ToString("hh:mm:ss tt");
                    String datepart = LastLoginDate.ToString("dd-MMM-yyyy");
                    lastlogindate = datepart + " at " + TimeIn12Format;
                }

                // generobj.WriteError("3B GET HERE AT  " + DateTime.Now.ToString());
                int passswchanged = cusauth.Passwordchanged ?? 0;
                string availablebalance = "0.00";
                string startstrign = DateTime.Now.AddDays(-30).ToString("yyyyMMdd");
                string endstartstng = DateTime.Now.AddDays(1).ToString("yyyyMMdd");
                string daterage = startstrign + " " + endstartstng;
                var corpmodel = new CorporateUserModel
                {
                    Username = cusauth.Username,
                    FullName = cusauth.FirstName +" "+ cusauth.LastName,
                    Email = cusauth.Email,
                    Phone1 = cusauth.Phone1,
                    CustomerID = mycorpr.CustomerId,
                    CorporateCustomerId = corpcust.Id.ToString(),
                };
               
                var tokenString = TokenGenerator.GenerateJSONWebToken(corpmodel,_config);
                string Tokenstring = tokenString;

                var loginlog = new TblLoginLogCorp
                {
                    Id = Guid.NewGuid(),
                    CustAuth = cusauth.Id,
                    LoginTime = DateTime.Now,
                    NotificationStatus = 0,
                    Channel = "Web"
                };
               
                //_context.SaveChanges();

                var tknblack = new TblTokenBlackCorp
                {
                    Id = Guid.NewGuid(),
                    CustAutId = cusauth.Id,
                    TokenCode = Tokenstring.Trim(),
                    DateGenerated = DateTime.Now,
                    IsBlack = 0
                };

                cusauth.LastLogin = DateTime.Now;
                cusauth.LastActivity = DateTime.Now;
                cusauth.Loggon = 1;
                cusauth.NoOfWrongAttempts = 0;
                cusauth.SendLoginEmail = 1;
                var audit = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Login),
                    Ipaddress = payLoad.ClientStaffIPAddress,
                    ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                    Macaddress = payLoad.MACAddress,
                    HostName = payLoad.HostName,
                    NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
                    $"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone1}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = cusauth.Id,
                    Username = cusauth.Username,
                    Description = "Login Attempt Successful",
                    TimeStamp = DateTime.Now
                };
                
                UnitOfWork.LoginLogCorporate.Add(loginlog);
                UnitOfWork.TokenBlackCorporateRepo.Add(tknblack);
                UnitOfWork.AuditTrialRepo.Add(audit);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(cusauth);
                UnitOfWork.Complete();
                payLoad.Password = "";
                _logger.LogInformation("Login Attempt Successful {0}", JsonConvert.SerializeObject(payLoad));

                string myCompanyName = corpcust.CompanyName;
                int isIndemnitySigned = cusauth.IndemnitySigned ?? 0;
                //send login message
                string fullName = cusauth.LastName + " " + cusauth.MiddleName + " " + cusauth.FirstName;
                ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.LoginMail(cusauth.Email, fullName, "")));
                var userRole = UnitOfWork.CorporateRoleRepo.GetCorporateRoleName(cusauth.CorporateRole.ToString());
                var permissions = UnitOfWork.CorporateUserRoleAccessRepo.GetCorporateUserPermissions(cusauth.CorporateRole.ToString());
                return Ok(new LoginResponsedata
                {
                    Responsecode = "00",
                    ResponseDescription = "success",
                    UserId = cusauth.Id,
                    UserpasswordChanged = passswchanged,
                    CustomerIdentity = cusauth.Username,
                    DefaultAccountBalance = availablebalance,
                    DefaultAccountName = corpcust.DefaultAccountName,
                    DefaultAccountNumber = corpcust.DefaultAccountNumber,
                    Phone = cusauth.Phone1, 
                    SecurityQuestion = "",
                    LastLoginDate = lastlogindate,
                    access_token = Tokenstring,
                    IndemnitySigned = isIndemnitySigned,
                    CustomerID = corpcust.CustomerId,
                    CompanyName = myCompanyName,
                    Status = cusauth.Status,
                    RegStage = cusauth.RegStage,
                    RoleId = cusauth.CorporateRole.ToString(),
                    Role = userRole,
                    Permissions = permissions,
                    CorporateCustomerId = cusauth.CorporateCustomerId,
                    AuthorizationType = corpcust.AuthorizationType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        // <summary>
        // Forgot Password
        // </summary>
        // <param name="email">Forgot password email</param>
        // <returns>tokem  </returns>
        // <response code="200">Returns a boolean value indicating where the reset mail has been sent successful</response>
        // <response code="400">If the item is null </response>     
        [HttpPost("ForgotPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> ForgetPassword([FromBody] ForgetPassword model)
        {
        try
            {
                bool isSent = false;

                if (model == null)
                {
                    return BadRequest("Model is empty");
                }

                if (string.IsNullOrEmpty(model.Email))
                {
                    return BadRequest("Email address is required");
                }

                var payload = new ForgetPassword
                {
                    //Answer = Encryption.DecryptStrings(model.Answer),
                   // SecurityQuestion = Encryption.DecryptStrings(model.SecurityQuestion),
                    CustomerId = Encryption.DecryptStrings(model.CustomerId),
                    Email = Encryption.DecryptStrings(model.Email),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };
                //var userEmail = Encryption.DecryptStrings(email);
                var validator = new ForgotPasswordValidation();
                var results = validator.Validate(payload);
                if (!results.IsValid)
                {
                    _logger.LogInformation("Invalid Request Data {0}",JsonConvert.SerializeObject(results));
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetCustomerByCustomerId(payload.CustomerId);
                if(tblCorporateCustomer == null)
                {
                    return BadRequest("Customer id is invalid");
                }
                var entity = UnitOfWork.CorporateProfileRepo.GetProfileByEmailAndCustomerId(payload.Email,tblCorporateCustomer.Id );
                if (entity != null)
                {
                    return BadRequest("Invalid Email");
                }
                var currentDate = DateTime.Now;
                string code = TokenGenerator.GenerateOTP();
                var tblPasswordReset = new TblPasswordReset
                {
                    Id = Guid.NewGuid(),
                    AuthId = entity.Id,
                    DateGenerated = currentDate,
                    ResetCode = code,
                    Status = 0
                };
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Password_Reset),
                    Ipaddress = payload.IPAddress,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    Macaddress = payload.MACAddress,
                    HostName = payload.HostName,
                    NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                    $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}, Status: {ProfileStatus.Active}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = entity.Id,
                    Username = entity.Username,
                    Description = "Forgot password and reset code triggered"
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.PasswordResetRepo.Add(tblPasswordReset);
                UnitOfWork.Complete();
                string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
                ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.PasswordResetMail(entity.Email,fullName,code)));
                isSent = true;
                return Ok(isSent);
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
                return StatusCode(500,new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        // <summary>
        // Reset Password
        // </summary>
        // <param name="model">Reset Password model</param>
        // <returns>Returns a boolean value indicating where the password reset was successful  </returns>
        // <response code="200">Returns a boolean value indicating where the password reset was successful</response>
        // <response code="400">If the item is null </response>     
        [HttpPut("ResetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> ResetPassword(ResetPasswordModel model)
        {
            try
                {
                    bool isSent = false;

                    if (model == null)
                    {
                        return BadRequest("Invalid model");
                    }
                    var payload = new ResetPasswordModel
                    {
                        CustomerId = Encryption.DecryptStrings(model.CustomerId),
                        Email = Encryption.DecryptStrings(model.Email),
                        Password = Encryption.DecryptStrings(model.Password),
                        Code = Encryption.DecryptStrings(model.Code),
                        ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                        IPAddress =  Encryption.DecryptStrings(model.IPAddress),
                        MACAddress =  Encryption.DecryptStrings(model.MACAddress),
                        HostName =  Encryption.DecryptStrings(model.HostName),
                    };

                    // call validator
                    var validator = new ResetPasswordValidation();
                    var results = validator.Validate(payload);
                    if (!results.IsValid)
                    {
                        _logger.LogInformation("Invalid Request Data {0}",JsonConvert.SerializeObject(results));
                        return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                    }

                    var getCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetCustomerByCustomerId(payload.CustomerId);
                    if (getCorporateCustomer == null)
                    {
                        return BadRequest("Corporate Customer is invalid"); ;
                    }

                    var entity = UnitOfWork.CorporateProfileRepo.GetProfileByEmailAndCustomerId(payload.Email, getCorporateCustomer.Id);
                    if (entity == null)
                    {
                        return BadRequest("Corporate Profile is invalid"); ;
                    }


                    var currentDate = DateTime.Now;
                    var tblPasswordReset = UnitOfWork.PasswordResetRepo.Find(x => x.AuthId.Equals(entity.Id) && x.ResetCode.Equals(model.Code) && x.Status == 0 && x.DateGenerated.Value.AddDays(24).AddMinutes(-1) > currentDate);
                    if(tblPasswordReset == null)
                    {
                        return BadRequest("Invalid or expired code");
                    }

                    tblPasswordReset.Status = 1;
                    tblPasswordReset.ResetDate = currentDate;
                    entity.Password = Encryption.EncriptPassword(payload.Password);

                    if (!ValidatePasswordChangeHistory(payload.Password, entity.Id.ToString()))
                    {
                        return BadRequest("Password must not match a previously used password");
                    } 
                    var auditTrail = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Password_Reset),
                        Ipaddress = payload.IPAddress,
                        ClientStaffIpaddress = payload.ClientStaffIPAddress,
                        Macaddress = payload.MACAddress,
                        HostName = payload.HostName,
                        NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = entity.Id,
                        Username = entity.Username,
                        Description = "Password reset successful. New password created"
                    };
            
                    UnitOfWork.AuditTrialRepo.Add(auditTrail);
                    UnitOfWork.PasswordHistoryRepo.Add(new TblPasswordHistory { Id = Guid.NewGuid(), CustomerProfileId = entity.Id.ToString(),Password = model.Password});
                    UnitOfWork.PasswordResetRepo.UpdatePasswordReset(tblPasswordReset);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                    UnitOfWork.Complete();
                    string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
                    ThreadPool.QueueUserWorkItem(_ =>_emailService.SendEmail(EmailTemplate.PasswordResetChangeMail(entity.Email,fullName)));
                    isSent = true;
                    return Ok(isSent);
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
                return StatusCode(500,new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        // <summary>
        // GetSecurityQuestions
        // </summary>
        // <param name="model">Change Password model</param>
        // <returns>Returns a boolean value indicating where the password change was successful  </returns>
        // <response code="200">Returns a boolean value indicating where the password change was successful</response>
        // <response code="400">If the item is null </response>     
        [HttpGet("GetSecurityQuestions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetSecurityQuestions()
        {
            try
            {
                var entity = await UnitOfWork.SecurityQuestionRepo.ListAllAsync();
                if (entity == null)
                {
                    return BadRequest("Current password or username is invalid");
                }
                var mapResponse = _mapper.Map<List<SecurityQuestionResponseDto>>(entity);
                return Ok(new ListResponseDTO<SecurityQuestionResponseDto>(_data:mapResponse,success:true, _message:Message.Success) );
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
                return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
              _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpGet("GetProfileSecurityQuestions")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<List<SecurityQuestionDto>> GetBankProfileSecurityQuestions(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest("Email address is required");
                }

                //call method to decrypt payload
                email = Encryption.DecryptStrings(email);
                var entity = UnitOfWork.BankProfileRepo.Find(x => x.Email.Equals(email));
                if (entity == null)
                {
                    return BadRequest("Email address is invalid");
                }
                var securityQuestions = new List<SecurityQuestionDto>
                {
                    new SecurityQuestionDto { Question = entity.SecurityQuestion },
                    new SecurityQuestionDto { Question = entity.SecurityQuestion2 },
                    new SecurityQuestionDto { Question = entity.SecurityQuestion3 }
                };
                return Ok(securityQuestions);
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
           
            }
        }

        /// <summary>
        /// SetSecurityQuestion
        /// </summary>
        /// <param name="model">Set security question model</param>
        /// <returns>Returns a boolean value indicating whether operation was successful or not</returns>
        /// <response code="200">Returns a boolean value indicating where the password reset was successful</response>
        /// <response code="400">If the item is null </response>
        [HttpPut("SetSecurityQuestion")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> SetSecurityQuestion([FromBody] SetSecurityQuestionDto model)
        {
            string errorMsg = string.Empty;
            try
            {
                if (model == null)
                {
                    return BadRequest("Model is empty");
                }

                if (string.IsNullOrEmpty(model.UserName))
                {
                    return BadRequest("User name is required");
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest("Security question is required");
                }

                if (string.IsNullOrEmpty(model.SecurityQuestionId))
                {
                    return BadRequest("Security question 1 is required");
                }

                if (string.IsNullOrEmpty(model.Answer))
                {
                    return BadRequest("Answer 1 is required");
                }

                if (string.IsNullOrEmpty(model.SecurityQuestionId2))
                {
                    return BadRequest("Security question 2 is required");
                }

                if (string.IsNullOrEmpty(model.Answer2))
                {
                    return BadRequest("Answer 2 is required");
                }

                if (string.IsNullOrEmpty(model.SecurityQuestionId3))
                {
                    return BadRequest("Security question 3 is required");
                }
                var payLoad = new SetSecurityQuestion
                {
                    Answer = Encryption.DecryptStrings(model.Answer),
                    Answer2 = Encryption.DecryptStrings(model.Answer2),
                    Answer3 = Encryption.DecryptStrings(model.Answer3),
                    CustomerId = Encryption.DecryptStrings(model.CustomerId),
                    Password = Encryption.DecryptStrings(model.Password),
                    SecurityQuestionId = Encryption.DecryptInt(model.SecurityQuestionId),
                    SecurityQuestionId2 = Encryption.DecryptInt(model.SecurityQuestionId2),
                    SecurityQuestionId3 = Encryption.DecryptInt(model.SecurityQuestionId3),
                    UserName = Encryption.DecryptStrings(model.UserName),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };

                // var validator = new SetSecurityQuestionValidation();
                // var results = validator.Validate(payLoad);
                // if (!results.IsValid)
                // {
                //     _logger.LogInformation("Invalid Request Data {0}",JsonConvert.SerializeObject(results));
                //     return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                // }


                if (payLoad.SecurityQuestionId == 0)
                {
                    return BadRequest("Security question 1 is required");
                }

                if (payLoad.SecurityQuestionId2 == 0)
                {
                    return BadRequest("Security question 2 is required");
                }

                if (payLoad.SecurityQuestionId3 == 0)
                {
                    return BadRequest("Security question 3 is required");
                }

                var tblSecurityQuestion = UnitOfWork.SecurityQuestionRepo.GetById(payLoad.SecurityQuestionId);
                if (tblSecurityQuestion == null)
                {
                    return BadRequest("Invalid security question id");
                }

                var tblSecurityQuestion2 = UnitOfWork.SecurityQuestionRepo.GetById(payLoad.SecurityQuestionId2);
                if (tblSecurityQuestion2 == null)
                {
                    return BadRequest("Invalid security question 2 id");
                }

                var tblSecurityQuestion3 = UnitOfWork.SecurityQuestionRepo.GetById(payLoad.SecurityQuestionId3);
                if (tblSecurityQuestion3 == null)
                {
                    return BadRequest("Invalid security question 3 id");
                }

                var getCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetCustomerByCustomerId(payLoad.CustomerId);
                if (getCorporateCustomer == null)
                {
                    return BadRequest("Corporate Customer is invalid"); ;
                }

                var entity = UnitOfWork.CorporateProfileRepo.GetProfileByUserNameAndCustomerId(payLoad.UserName, getCorporateCustomer.Id);
                if (entity == null)
                {
                    return BadRequest("Corporate Profile is invalid"); ;
                }

                // var entity = UnitOfWork.CorporateProfileRepo. (x => x.Username.Equals(payLoad.UserName));
                // if (entity == null)
                // {
                //    return BadRequest("Invalid credentials");
                // }
                if (entity.NoOfWrongAttempts == 3 || entity.NoOfWrongAttempts > 3)
                {
                    entity.ReasonsForDeactivation = "Multiple incorrect login attempt";
                    entity.Status = (int)ProfileStatus.Deactivated;
                    var audit = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
                        Ipaddress = payLoad.IPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = entity.Id,
                        Username = entity.Username,
                        Description = "Security Question Setting Failure. Multiple Invalid Password"
                    };
                    UnitOfWork.AuditTrialRepo.Add(audit);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                    UnitOfWork.Complete();
                    return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
                }
               
                var emppass = Encryption.OpenSSLDecrypt(entity.Password, Encryption.GetEncrptionKey());
                
                if(emppass != payLoad.Password)
                {
                    int wrontloginatempt = entity.NoOfWrongAttempts ?? 0;
                    entity.NoOfWrongAttempts = wrontloginatempt + 1;
                    entity.LastLoginAttempt = DateTime.Now;
                    var audit = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
                        Ipaddress = payLoad.IPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = entity.Id,
                        Username = entity.Username,
                        Description = "Security Question Setting Failure. Invalid Password"
                    };
                    UnitOfWork.AuditTrialRepo.Add(audit);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                    UnitOfWork.Complete();
                    return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "UserName Or Password is invalid", UserpasswordChanged = 0, CustomerIdentity = "" });
                }
                
                entity.RegStage = 2;
                entity.SecurityAnswer = model.Answer;
                entity.SecurityQuestion = tblSecurityQuestion.Question;
                entity.SecurityAnswer2 = model.Answer2;
                entity.SecurityAnswer3 = model.Answer3;
                entity.SecurityQuestion2 = tblSecurityQuestion2.Question;
                entity.SecurityQuestion3 = tblSecurityQuestion3.Question;
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
                    Ipaddress = payLoad.IPAddress,
                    ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                    Macaddress = payLoad.MACAddress,
                    HostName = payLoad.HostName,
                    NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                    $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = entity.Id,
                    Username = entity.Username,
                    Description = "Security Question Setting Successful"
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.Complete();
                return Ok(true);
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
                return StatusCode(500,new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        // <summary>
        // First time login password change
        // </summary>
        // <param name="model">First Login Password Change Model</param>
        // <returns>Returns a boolean value indicating where the password change was successful  </returns>
        // <response code="200">Returns a boolean value indicating where the password change was successful</response>
        // <response code="400">If the item is null </response>     
        [HttpPost("FirstLoginPasswordChange")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> FirstLoginPasswordChange([FromBody]FirstLoginPasswordChangeModel  model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Invalid model");
                }

                if (string.IsNullOrEmpty(model.NewPassword.Trim()))
                {
                    return BadRequest("Please enter a valid new password");
                }

                var payLoad = new FirstLoginPasswordChangeModel
                {
                    CustomerId = Encryption.DecryptStrings(model.CustomerId),
                    UserName = Encryption.DecryptStrings(model.UserName),
                    CurrentPassword = Encryption.DecryptStrings(model.CurrentPassword),
                    NewPassword = Encryption.DecryptStrings(model.NewPassword),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };

                var validator = new FirstLoginPasswordChangeValidation();
                var results = validator.Validate(payLoad);
                if (!results.IsValid)
                {
                    _logger.LogInformation("Invalid Request Data {0}",JsonConvert.SerializeObject(results));
                    return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false,_validationResult: results.Errors));
                }

                var getCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetCustomerByCustomerId(payLoad.CustomerId);
                if (getCorporateCustomer == null)
                {
                    return BadRequest("Corporate Customer Dosnt exist"); ;
                }

                var entity = UnitOfWork.CorporateProfileRepo.GetProfileByUserNameAndCustomerId(payLoad.UserName, getCorporateCustomer.Id);
                if (entity == null)
                {
                    return BadRequest("Corporate Profile is invalid"); ;
                }

                if (entity.Passwordchanged == 1 && entity.ResetInitiated != 1)
                {
                    if(entity.PasswordExpiryDate != null && entity.PasswordExpiryDate.Value < DateTime.Now.AddDays(-30))
                    {

                    }
                    else
                    {
                        return BadRequest("First time login or password reset initiation is not detected");
                        // responseModel.Message = "First time login, password expiry update or password reset initiation is not detected";
                        // return Ok(responseModel);
                    }
                }

                if (entity.Passwordchanged == 1 && entity.ResetInitiated != 1)
                {
                    return BadRequest("First time login or password reset initiation is not detected"); ;
                }

                string decryptPassword = Encryption.DecriptPassword(entity.Password);

                if (!decryptPassword.Equals(payLoad.CurrentPassword))
                {
                    entity.NoOfWrongAttempts += 1;
                    if (entity.NoOfWrongAttempts == 3)
                    {
                        entity.ReasonsForDeactivation = "Multiple incorrect login attempt";
                        entity.Status = (int)ProfileStatus.Deactivated;
                        var auditrail = new TblAuditTrail
                        {
                            Id = Guid.NewGuid(),
                            ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
                            Ipaddress = payLoad.IPAddress,
                            ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                            Macaddress = payLoad.MACAddress,
                            HostName = payLoad.HostName,
                            NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                            $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                            PreviousFieldValue = "",
                            TransactionId = "",
                            UserId = entity.Id,
                            Username = entity.Username,
                            Description = "First time password change failure. Multiple incorrect password"
                        };
                        UnitOfWork.AuditTrialRepo.Add(auditrail);
                        UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                        UnitOfWork.Complete();
                        return BadRequest("Multiple incorrect login attempted. Your account has been deactivated");
                    }
                    var audit = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
                        Ipaddress = payLoad.IPAddress,
                        ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                        Macaddress = payLoad.MACAddress,
                        HostName = payLoad.HostName,
                        NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                        $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = entity.Id,
                        Username = entity.Username,
                        Description = "First time password change failure. Invalid current password or username"
                    };
                    UnitOfWork.AuditTrialRepo.Add(audit);
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                    UnitOfWork.Complete();
                    return BadRequest("Current password or username is invalid");
                }

                if (entity.DateCompleted != null && ((DateTime)entity.DateCompleted.Value.AddDays(1)) < DateTime.Now)
                {
                    return BadRequest("Your password seems to have expired. Please contact your admin or bank admin");
                }

                if (entity.Username.Contains(model.NewPassword))
                {
                    return BadRequest("New password must not contain username");
                }
                // UnitOfWork..Add(new TblPasswordHistory() { Id = Guid.NewGuid(), CustomerProfileId = entity.Id.ToString(), Password = entity.Password });
                entity.Password = Encryption.EncriptPassword(payLoad.NewPassword);
                entity.Passwordchanged = 1;
                entity.ResetInitiated = 0;
                entity.PasswordExpiryDate = DateTime.Now.AddDays(30);

                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
                    Ipaddress = payLoad.IPAddress,
                    ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                    Macaddress = payLoad.MACAddress,
                    HostName = payLoad.HostName,
                    NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                    $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = entity.Id,
                    Username = entity.Username,
                    Description = "First time password change success. New password created"
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.Complete();
                string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
                ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.PasswordResetChangeMail(entity.Email, fullName)));
                if (entity.RegStage == null || entity.RegStage < 2)
                {
                    return Ok(new LoginResponsedata { Responsecode = "16", ResponseDescription = "Sorry, we noticed you are yet to setup your security question.  You need to setup a security question and answer before you can login", UserpasswordChanged = 0, CustomerIdentity = "" });
                }
                return Ok(true);
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
                return StatusCode(500,new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
              _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
    
        [HttpPost("ChangePassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> ChangePassword([FromBody] ChangeUserPasswordParam model)
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


                if (model == null)
                {
                return BadRequest("Invalid model");
                }

                if (string.IsNullOrEmpty(model.NewPassword.Trim()))
                {
                return BadRequest("Please enter a valid new password");
                }

                var payLoad = new ChangeUserPasswordParam
                {
                    Id = Encryption.DecryptStrings(model.Id),
                    OldPassword = Encryption.DecryptStrings(model.OldPassword),
                    NewPassword = Encryption.DecryptStrings(model.NewPassword),
                    ComfirmPassword = Encryption.DecryptStrings(model.ComfirmPassword),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName)
                };

                if (CorporateProfile == null)
                {
                    return BadRequest("Invalid corporate id");
                }

                 
                var entity = UnitOfWork.CorporateProfileRepo.GetByIdAsync(CorporateProfile.Id);
                if (entity == null)
                {
                    return BadRequest("Invalid User");
                }
                
                string oldPassword = payLoad.OldPassword;
                string newPassword = payLoad.NewPassword;
                string dbPassword = Encryption.DecriptPassword(entity.Password);

                var checkPasswordInfo = PasswordValidator.ValidatePassword(newPassword);
                if(!string.IsNullOrEmpty(checkPasswordInfo))
                {
                    return BadRequest(checkPasswordInfo);
                }
                if (dbPassword.Equals(newPassword))
                {
                return BadRequest("Password Already Exit kindly create a new one");
                }

                if (dbPassword != oldPassword)
                {
                entity.NoOfWrongAttempts += 1;
                if (entity.NoOfWrongAttempts == 3)
                {
                    entity.ReasonsForDeactivation = "Multiple incorrect login attempt";
                    entity.Status = (int) ProfileStatus.Deactivated;
                    UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                    UnitOfWork.Complete();
                    return BadRequest("Multiple incorrect login attempted. Your account has been deactivated");
                }
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.Complete();
                return BadRequest("Incorrect password");
                }
                entity.Password = Encryption.EncriptPassword(newPassword);
                entity.Passwordchanged = 1;
                entity.ResetInitiated = 0;
                entity.PasswordExpiryDate = DateTime.Now.AddDays(30);

                var auditTrail = new TblAuditTrail
                {
                Id = Guid.NewGuid(),
                ActionCarriedOut = nameof(AuditTrailAction.Change_Password),
                Ipaddress = payLoad.IPAddress,
                ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
                Macaddress = payLoad.MACAddress,
                HostName = payLoad.HostName,
                NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
                $"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone1}",
                PreviousFieldValue = "",
                TransactionId = "",
                UserId = entity.Id,
                Username = entity.Username,
                Description = "Corporate Admin has update his password success. New password Updated"
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(entity);
                UnitOfWork.Complete();
                return Ok(true);
            }
            catch (DbUpdateException ex)
            {
              if (ex.InnerException != null)
              {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
              }
              return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }

        [HttpPost("LogOut")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> LogOut()
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
                if(CorporateProfile == null)
                {
                    return StatusCode(401, "User is not authenticated");
                }
                var removeToken = new List<TblTokenBlackCorp>();
                var tokenBlack = UnitOfWork.TokenBlackCorporateRepo.GetBlackTokenById(CorporateProfile.Id);
                if(tokenBlack.Count != 0)
                {
                    foreach (var mykn in tokenBlack)
                    {
                        mykn.IsBlack = 1;
                        removeToken.Add(mykn);
                    }
                }

                if (removeToken.Count == 0)
                {
                  return Ok(true);
                }

                UnitOfWork.TokenBlackCorporateRepo.RemoveRange(removeToken);
                UnitOfWork.Complete();
                return Ok(true);
            }
            catch (DbUpdateException ex)
            {
              if (ex.InnerException != null)
              {
                var sqlException = ex.InnerException.InnerException;
                _logger.LogError("DATABASE ERROR:",Formater.JsonType(sqlException));
              }
              return BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus:false));
            }
            catch (Exception ex)
            {
               _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
            }
        }
        private bool ValidatePasswordChangeHistory(string password, string customerProfileId)
        {
            var tblPasswordHistory = UnitOfWork.PasswordHistoryRepo.GetPasswordHistoryByCorporateProfileId(customerProfileId);
            if (tblPasswordHistory == null || tblPasswordHistory?.Count == 0)
            {
                return true;
            }
            else
            {
                foreach(var item in tblPasswordHistory)
                {
                    if(Encryption.DecriptPassword(item.Password) == password)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
