using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.AuditTrial.Dto;
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
using CIB.Core.Common.Dto;
using CIB.Core.Services.Authentication.Dto;
using CIB.Core.Services.Authentication;
using CIB.Core.Common;
using System.Security.Claims;
using System.Linq;

namespace CIB.BankAdmin.Controllers
{
	[ApiController]
	[Route("api/BankAdmin/v1/[controller]")]
	public class AccountController : BaseAPIController
	{
		protected readonly IConfiguration _config;
		protected readonly IEmailService _emailService;
		protected readonly IApiService _apiService;
		protected readonly IToken2faService _2fa;
		private readonly ILogger<AccountController> _logger;
		public AccountController(
				ILogger<AccountController> _logger,
				IConfiguration config,
				IUnitOfWork unitOfWork,
				IMapper mapper,
				IEmailService emailService,
				IApiService apiService,
				IHttpContextAccessor accessor,
				IToken2faService _2fa, IAuthenticationService authService) : base(mapper, unitOfWork, accessor, authService)
		{
			_config = config;
			_emailService = emailService;
			_apiService = apiService;
			this._2fa = _2fa;
			this._logger = _logger;
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
		public async Task<ActionResult<LoginResponsedata>> BankUserLogin([FromBody] GenericRequestDto login)
		{
			try
			{
				if (string.IsNullOrEmpty(login.Data))
				{
					return BadRequest("invalid request");
				}

				var requestData = JsonConvert.DeserializeObject<UserData>(Encryption.DecryptStrings(login.Data));
				if (requestData == null)
				{
					return BadRequest("invalid request data");
				}
				var payLoad = new BankUserLoginParam
				{
					Username = requestData.Username,
					Password = requestData.Password,
					Token = requestData.Token,
					ClientStaffIPAddress = Encryption.DecryptStrings(login.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(login.IPAddress),
					MACAddress = Encryption.DecryptStrings(login.MACAddress),
					HostName = Encryption.DecryptStrings(login.HostName)
				};

				var validator = new BankLoginValidation();
				var results = validator.Validate(payLoad);
				if (!results.IsValid)
				{
					_logger.LogInformation("Invalid Request Data {0}", results);
					return UnprocessableEntity(new ValidatorResponse(_data: new Object(), _success: false, _validationResult: results.Errors));
				}


				var userName = $"{payLoad.Username}";
				var validOTP = await _2fa.TokenAuth(userName, payLoad.Token);
				if (validOTP.ResponseCode != "00")
				{
					_logger.LogError("2FA API ERROR {0}", $"{validOTP.ResponseMessage}");
					return BadRequest(validOTP.ResponseMessage);
				}

				var authResult = await _apiService.ADLogin(payLoad.Username, payLoad.Password);
				payLoad.Password = "";
				payLoad.Token = "";
				if (!authResult.IsAuthenticated)
				{
					_logger.LogInformation("AD Authentication Failed {0}", JsonConvert.SerializeObject(authResult));
					var bankUser = UnitOfWork.BankAuthenticationRepo.BankUserLogin(payLoad);
					if (bankUser == null)
					{
						_logger.LogInformation("you are not profile on this application {0}", JsonConvert.SerializeObject(payLoad));
						return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "you are not profile on this application, Please Contact Bank Admin", UserpasswordChanged = 0, CustomerIdentity = "" });
					}
					if (bankUser.NoOfWrongAttempts == 3 || bankUser.NoOfWrongAttempts > 3)
					{

						bankUser.ReasonsForDeactivation = "Multiple incorrect login attempt";
						bankUser.Status = -1;
						var auditt = new TblAuditTrail
						{
							Id = Guid.NewGuid(),
							ActionCarriedOut = nameof(AuditTrailAction.Login),
							Ipaddress = payLoad.IPAddress,
							ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
							Macaddress = payLoad.MACAddress,
							HostName = payLoad.HostName,
							NewFieldValue = $"First Name: {bankUser.FirstName}, Last Name: {bankUser.LastName}, Username: {bankUser.Username}, Email Address:  {bankUser.Email}, " +
								$"Middle Name: {bankUser.MiddleName}, Phone Number: {bankUser.Phone}",
							PreviousFieldValue = "",
							TransactionId = "",
							UserId = bankUser.Id,
							Username = bankUser.Username,
							Description = "Login Attempt Failure. Multiple incorrect login",
							TimeStamp = DateTime.Now
						};
						UnitOfWork.AuditTrialRepo.Add(auditt);
						UnitOfWork.BankProfileRepo.UpdateBankProfile(bankUser);
						UnitOfWork.Complete();
						_logger.LogInformation("Login Attempt Failure. Multiple incorrect login {0}", JsonConvert.SerializeObject(payLoad));
						return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
					}
					int wrontloginatempt = bankUser.NoOfWrongAttempts ?? 0;
					bankUser.NoOfWrongAttempts = wrontloginatempt + 1;
					bankUser.LastLoginAttempt = DateTime.Now;
					var audit = new AuditTrialDto
					{
						ActionCarriedOut = nameof(AuditTrailAction.Login),
						Ipaddress = payLoad.IPAddress,
						ClientStaffIPAddress = payLoad.ClientStaffIPAddress,
						MACAddress = payLoad.MACAddress,
						HostName = payLoad.HostName,
						NewFieldValue = $"First Name: {bankUser.FirstName}, Last Name: {bankUser.LastName}, Username: {bankUser.Username}, Email Address:  {bankUser.Email}, " +
							$"Middle Name: {bankUser.MiddleName}, Phone Number: {bankUser.Phone}",
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = bankUser.Id,
						Username = bankUser.Username,
						Description = "Login Attempt Failure. Incorrect Password",
						TimeStamp = DateTime.Now
					};
					var audtrl = Mapper.Map<TblAuditTrail>(audit);
					UnitOfWork.AuditTrialRepo.Add(audtrl);
					UnitOfWork.BankProfileRepo.UpdateBankProfile(bankUser);
					UnitOfWork.Complete();
					_logger.LogInformation("Invalid login attempt {0}", JsonConvert.SerializeObject(payLoad));
					return BadRequest(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Invalid login attempt", UserpasswordChanged = 0, CustomerIdentity = "" });
				}


				var cusauth = UnitOfWork.BankAuthenticationRepo.BankUserLogin(payLoad);
				if (cusauth == null)
				{
					payLoad.Password = "";
					payLoad.Token = "";
					_logger.LogInformation("you are not profile on this application, Please Contact Bank Admin {0}", JsonConvert.SerializeObject(payLoad));
					return BadRequest(new LoginResponsedata { Responsecode = ResponseCode.NOT_PROFILE, ResponseDescription = "you are not profile on this application, Please Contact Bank Admin", UserpasswordChanged = 0, CustomerIdentity = "" });
				}

				if (cusauth.Status == (int)ProfileStatus.Deactivated)
				{
					payLoad.Password = "";
					var audit = new AuditTrialDto
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Login),
						Ipaddress = payLoad.ClientStaffIPAddress,
						IPAddress = payLoad.ClientStaffIPAddress,
						MACAddress = payLoad.MACAddress,
						HostName = payLoad.HostName,
						NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
							$"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone},Status: {ProfileStatus.Deactivated}",
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = cusauth.Id,
						Username = cusauth.Username,
						Description = "Login Attempt Failure. Profile Deactivated",
						TimeStamp = DateTime.Now
					};
					var audtrl = Mapper.Map<TblAuditTrail>(audit);
					UnitOfWork.AuditTrialRepo.Add(audtrl);
					UnitOfWork.Complete();
					_logger.LogInformation("your profile has been deactivated {0}", JsonConvert.SerializeObject(payLoad));
					return BadRequest(new LoginResponsedata { Responsecode = ResponseCode.DEACTIVATED_PROFILE, ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
				}
				if (cusauth.NoOfWrongAttempts == 3 || cusauth.NoOfWrongAttempts > 3)
				{
					payLoad.Password = "";
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
							$"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone}",
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = cusauth.Id,
						Username = cusauth.Username,
						Description = "Login Attempt Failure. Multiple incorrect login",
						TimeStamp = DateTime.Now
					};
					UnitOfWork.AuditTrialRepo.Add(auditt);
					UnitOfWork.BankProfileRepo.UpdateBankProfile(cusauth);
					UnitOfWork.Complete();
					_logger.LogInformation("Login Attempt Failure. Multiple incorrect login {0}", JsonConvert.SerializeObject(payLoad));
					return BadRequest(new LoginResponsedata { Responsecode = ResponseCode.DEACTIVATED_PROFILE, ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
				}

				//check if last activity (90 days)
				if (cusauth.LastActivity != null && cusauth.LastActivity.Value < DateTime.Now.AddDays(-90))
				{
					payLoad.Password = "";
					cusauth.ReasonsForDeactivation = "Inactive for 90 days";
					cusauth.Status = -1;
					var audit = new TblAuditTrail
					{
						Id = Guid.NewGuid(),
						ActionCarriedOut = nameof(AuditTrailAction.Login),
						Ipaddress = payLoad.ClientStaffIPAddress,
						ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
						Macaddress = payLoad.MACAddress,
						HostName = payLoad.HostName,
						NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
							$"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone}",
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = cusauth.Id,
						Username = cusauth.Username,
						Description = "Login Attempt Failure. User disabled due to inactivity for about 90 days",
						TimeStamp = DateTime.Now
					};
					UnitOfWork.AuditTrialRepo.Add(audit);
					UnitOfWork.Complete();
					_logger.LogInformation("we noticed your account has been inactive for about 90 days and has been suspended. Please contact your bank admin. {0}", JsonConvert.SerializeObject(payLoad));
					return BadRequest(new LoginResponsedata { Responsecode = ResponseCode.INACTIVE_ACCOUNT, ResponseDescription = "Sorry, we noticed your account has been inactive for about 90 days and has been suspended. Please contact your bank admin.", UserpasswordChanged = cusauth.Passwordchanged ?? 0, CustomerIdentity = "" });
				}

				var tokenBlack = UnitOfWork.TokenBlackRepo.GetBlackTokenById(cusauth.Id);
				foreach (var mykn in tokenBlack)
				{
					mykn.IsBlack = 1;
					UnitOfWork.TokenBlackRepo.UpdateTokenBlack(mykn);
				}
				payLoad.Password = "";
				//UnitOfWork.TokenBlackCorporateRepo.RemoveRange((IEnumerable<TblTokenBlackCorp>)tokenBlack);
				string lastlogindate;
				if (cusauth.LastLogin == null)
				{
					lastlogindate = "";
				}
				else
				{
					var LastLoginDate = cusauth.LastLogin ?? DateTime.Now;
					var TimeIn12Format = LastLoginDate.ToString("hh:mm:ss tt");
					var datepart = LastLoginDate.ToString("dd-MMM-yyyy");
					lastlogindate = datepart + " at " + TimeIn12Format;
				}
				int passswchanged = cusauth.Passwordchanged ?? 0;
				var corpmodel = new CorporateUserModel
				{
					UserId = cusauth.Id.ToString(),
					Username = cusauth.Username,
					FullName = cusauth.FirstName,
					Email = cusauth.Email,
					Phone1 = cusauth.Phone
				};
				//var Tokenstring = TokenGenerator.GenerateJSONWebToken(corpmodel,_config);
				var tokenstring = AuthService.JWTAuthentication(corpmodel);
				var loginlog = new TblLoginLogCorp
				{
					Id = Guid.NewGuid(),
					CustAuth = cusauth.Id,
					LoginTime = DateTime.Now,
					NotificationStatus = 0,
					Channel = "Web"
				};

				tokenstring.RefreshToken = AuthService.GenerateRefreshToken();
				var tknblack = new TblTokenBlack
				{
					Id = Guid.NewGuid(),
					CustAutId = cusauth.Id,
					TokenCode = tokenstring.Token.Trim(),
					DateGenerated = DateTime.Now,
					RefreshToken = tokenstring.RefreshToken.Trim(),
					RefreshTokenExpiryTime = DateTime.Now.AddHours(24),
					IsBlack = 0
				};
				cusauth.LastLogin = DateTime.Now;
				cusauth.LastActivity = DateTime.Now;
				cusauth.Loggon = 1;
				cusauth.NoOfWrongAttempts = 0;
				cusauth.SendLoginEmail = 1;
				cusauth.ReasonsForDeactivation = "";
				cusauth.ReasonsForDeclining = "";
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Login),
					Ipaddress = payLoad.ClientStaffIPAddress,
					ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
					Macaddress = payLoad.MACAddress,
					HostName = payLoad.HostName,
					NewFieldValue = $"First Name: {cusauth.FirstName}, Last Name: {cusauth.LastName}, Username: {cusauth.Username}, Email Address:  {cusauth.Email}, " +
						$"Middle Name: {cusauth.MiddleName}, Phone Number: {cusauth.Phone}, Status: {ProfileStatus.Active}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = cusauth.Id,
					Username = cusauth.Username,
					Description = "Login Attempt Successful",
					TimeStamp = DateTime.Now
				};

				//string filePath = Path.Combine("htmlTemplate", "CustomerLogin.html");

				UnitOfWork.TokenBlackRepo.Add(tknblack);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.LoginLogCorporate.Add(loginlog);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(cusauth);
				UnitOfWork.Complete();
				_logger.LogInformation("Login Attempt Successful. {0}", Formater.JsonType(payLoad));
				//int isIndemnitySigned = cusauth.IndemnitySigned ?? 0;
				var fullName = cusauth.LastName + " " + cusauth.MiddleName + " " + cusauth.FirstName;
				//ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.LoginMail(cusauth.Email, fullName, "")));
				var dto = JsonConvert.SerializeObject(new LoginDto
				{
					responsecode = ResponseCode.SUCCESS,
					responseDescription = Message.Success,
					access_token = tokenstring.Token.Trim(),
					refresh_token = tokenstring.RefreshToken.Trim(),
					userId = cusauth.Id,
					userpasswordChanged = passswchanged,
					customerIdentity = cusauth.Username,
					phone = cusauth.Phone,
					securityQuestion = "",
					lastLoginDate = lastlogindate,
					regStage = cusauth.RegStage,
					status = cusauth.Status,
					roleId = cusauth.UserRoles,
					role = UnitOfWork.RoleRepo.GetRoleName(cusauth.UserRoles),

				});
				return Ok(new
				{
					Data = Encryption.EncryptStrings(dto),
					Permissions = UnitOfWork.UserAccessRepo.GetUserPermissions(cusauth.UserRoles?.ToString())
				});
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
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
					Email = Encryption.DecryptStrings(model.Email),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};
				//var userEmail = Encryption.DecryptStrings(email);
				var entity = UnitOfWork.BankProfileRepo.GetProfileByEmail(payload.Email);
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
					Ipaddress = payload.ClientStaffIPAddress,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
						$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}, Status: {ProfileStatus.Active}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = entity.Id,
					Username = entity.Username,
					Description = "Forgot password and reset code triggered",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.PasswordResetRepo.Add(tblPasswordReset);
				UnitOfWork.Complete();
				string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.PasswordResetMail(entity.Email, fullName, code)));
				isSent = true;
				return Ok(isSent);
			}
			catch (DbUpdateException ex)
			{
				var sqlException = ex.InnerException.InnerException;
				_logger.LogError("DATABASE ERROR:", Formater.JsonType(sqlException));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
		}
		// <summary>
		// Reset Password
		// </summary>
		// <param name="model">Reset Password model</param>
		// <returns>Returns a boolean value indicating where the password reset was successful  </returns>
		// <response code="200">Returns a boolean value indicating where the password reset was successful</response>
		// <response code="400">If the item is null </response>     
		[HttpPost("ResetPassword")]
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
					Email = Encryption.DecryptStrings(model.Email),
					Password = Encryption.DecryptStrings(model.Password),
					Code = Encryption.DecryptStrings(model.Code),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName),
				};

				var entity = UnitOfWork.BankProfileRepo.GetProfileByEmail(payload.Email);
				if (entity == null)
				{
					return BadRequest("Invalid Email");
				}
				var currentDate = DateTime.Now;
				var tblPasswordReset = UnitOfWork.PasswordResetRepo.Find(x => x.AuthId.Equals(entity.Id) && x.ResetCode.Equals(model.Code) && x.Status == 0 && x.DateGenerated.Value.AddDays(24).AddMinutes(-1) > currentDate);
				if (tblPasswordReset == null)
				{
					return BadRequest("Invalid or expired code");
				}
				tblPasswordReset.Status = 1;
				tblPasswordReset.ResetDate = currentDate;
				entity.Password = Encryption.EncriptPassword(payload.Password);
				var auditTrail = new TblAuditTrail
				{
					Id = Guid.NewGuid(),
					ActionCarriedOut = nameof(AuditTrailAction.Password_Reset),
					Ipaddress = payload.IPAddress,
					ClientStaffIpaddress = payload.ClientStaffIPAddress,
					Macaddress = payload.MACAddress,
					HostName = payload.HostName,
					NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
						$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = entity.Id,
					Username = entity.Username,
					Description = "Password reset successful. New password created",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.PasswordResetRepo.Add(tblPasswordReset);
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.Complete();
				string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.PasswordResetChangeMail(entity.Email, fullName)));
				isSent = true;
				return Ok(isSent);
			}
			catch (DbUpdateException ex)
			{
				var sqlException = ex.InnerException.InnerException;
				_logger.LogError("DATABASE ERROR:", Formater.JsonType(sqlException));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
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
		public async Task<ActionResult<ListResponseDTO<SecurityQuestionResponseDto>>> GetSecurityQuestions()
		{
			try
			{
				var entity = await UnitOfWork.SecurityQuestionRepo.ListAllAsync();
				if (entity == null)
				{
					return BadRequest("Current password or username is invalid");
				}
				var mapResponse = _mapper.Map<List<SecurityQuestionResponseDto>>(entity);
				return Ok(new ListResponseDTO<SecurityQuestionResponseDto>(_data: mapResponse, success: true, _message: Message.Success));
			}
			catch (DbUpdateException ex)
			{
				var sqlException = ex.InnerException.InnerException;
				_logger.LogError("DATABASE ERROR:", Formater.JsonType(sqlException));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
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
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
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
		public async Task<ActionResult<bool>> SetSecurityQuestion([FromBody] SetSecurityQuestionDto model)
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
					Password = Uri.EscapeDataString(Encryption.DecryptStrings(model.Password)),
					SecurityQuestionId = Encryption.DecryptInt(model.SecurityQuestionId),
					SecurityQuestionId2 = Encryption.DecryptInt(model.SecurityQuestionId2),
					SecurityQuestionId3 = Encryption.DecryptInt(model.SecurityQuestionId3),
					UserName = Encryption.DecryptStrings(model.UserName),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};

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
				var authResult = await _apiService.ADLogin(payLoad.UserName, payLoad.Password);
				var entity = UnitOfWork.BankProfileRepo.Find(x => x.Username.Equals(payLoad.UserName));
				if (!authResult.IsAuthenticated)
				{
					if (entity == null)
					{
						return Ok(new LoginResponsedata { Responsecode = "11", ResponseDescription = "Sorry, you have not been profile on this application, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
					}
					if (entity.NoOfWrongAttempts == 3 || entity.NoOfWrongAttempts > 3)
					{
						entity.ReasonsForDeactivation = "Multiple incorrect login attempt";
						entity.Status = 0;
						var audit = new TblAuditTrail
						{
							Id = Guid.NewGuid(),
							ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
							Ipaddress = payLoad.IPAddress,
							ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
							Macaddress = payLoad.MACAddress,
							HostName = payLoad.HostName,
							NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
								$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}",
							PreviousFieldValue = "",
							TransactionId = "",
							UserId = entity.Id,
							Username = entity.Username,
							Description = "Security Question Setting Failure. Multiple Invalid Password",
							TimeStamp = DateTime.Now
						};
						UnitOfWork.AuditTrialRepo.Add(audit);
						UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
						UnitOfWork.Complete();
						return BadRequest(new LoginResponsedata { Responsecode = ResponseCode.DEACTIVATED_PROFILE, ResponseDescription = "Sorry, your profile has been deactivated, please contact our support team", UserpasswordChanged = 0, CustomerIdentity = "" });
					}
					return Ok(new LoginResponsedata { Responsecode = ResponseCode.INVALID_ATTEMPT, ResponseDescription = "Invalid credentials", UserpasswordChanged = 0, CustomerIdentity = "" });
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
						$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = entity.Id,
					Username = entity.Username,
					Description = "Security Question Setting Successful",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.Complete();
				return Ok(true);
			}
			catch (DbUpdateException ex)
			{
				var sqlException = ex.InnerException.InnerException;
				_logger.LogError("DATABASE ERROR:", Formater.JsonType(sqlException));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
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
		public ActionResult<bool> FirstLoginPasswordChange([FromBody] FirstLoginPasswordChangeModel model)
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
					UserName = Encryption.DecryptStrings(model.UserName),
					CurrentPassword = Encryption.DecryptStrings(model.CurrentPassword),
					NewPassword = Encryption.DecryptStrings(model.NewPassword),
					ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
					IPAddress = Encryption.DecryptStrings(model.IPAddress),
					MACAddress = Encryption.DecryptStrings(model.MACAddress),
					HostName = Encryption.DecryptStrings(model.HostName)
				};

				var entity = UnitOfWork.BankProfileRepo.GetProfileByUserName(payLoad.UserName);
				if (entity == null)
				{
					return BadRequest("Current password or username is invalid"); ;
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
						entity.Status = -1;
						var auditrail = new TblAuditTrail
						{
							Id = Guid.NewGuid(),
							ActionCarriedOut = nameof(AuditTrailAction.Set_Security_Question),
							Ipaddress = payLoad.IPAddress,
							ClientStaffIpaddress = payLoad.ClientStaffIPAddress,
							Macaddress = payLoad.MACAddress,
							HostName = payLoad.HostName,
							NewFieldValue = $"First Name: {entity.FirstName}, Last Name: {entity.LastName}, Username: {entity.Username}, Email Address:  {entity.Email}, " +
								$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}",
							PreviousFieldValue = "",
							TransactionId = "",
							UserId = entity.Id,
							Username = entity.Username,
							Description = "First time password change failure. Multiple incorrect password",
							TimeStamp = DateTime.Now
						};
						UnitOfWork.AuditTrialRepo.Add(auditrail);
						UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
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
							$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}",
						PreviousFieldValue = "",
						TransactionId = "",
						UserId = entity.Id,
						Username = entity.Username,
						Description = "First time password change failure. Invalid current password or username",
						TimeStamp = DateTime.Now
					};
					UnitOfWork.AuditTrialRepo.Add(audit);
					UnitOfWork.BankProfileRepo.Add(entity);
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
						$"Middle Name: {entity.MiddleName}, Phone Number: {entity.Phone}",
					PreviousFieldValue = "",
					TransactionId = "",
					UserId = entity.Id,
					Username = entity.Username,
					Description = "First time password change success. New password created",
					TimeStamp = DateTime.Now
				};
				UnitOfWork.AuditTrialRepo.Add(auditTrail);
				UnitOfWork.BankProfileRepo.UpdateBankProfile(entity);
				UnitOfWork.Complete();
				string fullName = entity.LastName + " " + entity.MiddleName + " " + entity.FirstName;
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.PasswordResetChangeMail(entity.Email, fullName)));
				if (entity.RegStage == null || entity.RegStage < 2)
				{
					return Ok(new LoginResponsedata { Responsecode = ResponseCode.SECURITY_QUESTION, ResponseDescription = "Sorry, we noticed you are yet to setup your security question.  You need to setup a security question and answer before you can login", UserpasswordChanged = 0, CustomerIdentity = "" });
				}
				return Ok(true);
			}
			catch (DbUpdateException ex)
			{
				var sqlException = ex.InnerException.InnerException;
				_logger.LogError("DATABASE ERROR:", Formater.JsonType(sqlException));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
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
				if (BankProfile == null)
				{
					return StatusCode(401, "User is not authenticated");
				}
				var removeToken = new List<TblTokenBlack>();
				var tokenBlack = UnitOfWork.TokenBlackRepo.GetBlackTokenById(BankProfile.Id);
				if (tokenBlack.Count != 0)
				{
					foreach (var mykn in tokenBlack)
					{
						mykn.IsBlack = 1;
						removeToken.Add(mykn);
					}
				}
				var userId = User?.Identity?.Name;
				if (userId != null)
				{
					var user = UnitOfWork.TokenBlackRepo.GetTokenByUserId(Guid.Parse(userId));
					user.RefreshTokenExpiryTime = null;
					user.IsBlack = 1;
					user.TokenCode = null;
					user.RefreshToken = null;
					UnitOfWork.TokenBlackRepo.UpdateTokenBlack(user);
				}

				if (removeToken.Count == 0)
				{
					return Ok(true);
				}

				UnitOfWork.TokenBlackRepo.RemoveRange(removeToken);
				UnitOfWork.Complete();
				return Ok(true);
			}
			catch (DbUpdateException ex)
			{
				if (ex.InnerException != null)
				{
					var sqlException = ex.InnerException.InnerException;
					_logger.LogError("DATABASE ERROR:", Formater.JsonType(sqlException));
				}
				return BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: Message.ServerError, responseStatus: false));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		[HttpPost("refresh")]
		public IActionResult Refresh(Token token)
		{
			if (token is null) return BadRequest("Invalid client request");
			string accessToken = token.access_token;
			string refreshToken = token.refresh_token;
			var principal = AuthService.GetPrincipalFromExpiredToken(accessToken);
			if (principal is null) return BadRequest("Invalid token");
			IEnumerable<Claim> claim = principal.Claims;
			// var userId = ; //this is mapped to the Name claim by default

			var usernameClaim = claim.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

			var user = UnitOfWork.TokenBlackRepo.GetTokenByUserId(Guid.Parse(usernameClaim.Value));
			if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now) return BadRequest("Invalid client request");

			var cusauth = UnitOfWork.BankProfileRepo.GetByIdAsync(Guid.Parse(usernameClaim.Value));
			if (cusauth is null) return BadRequest("Invalid client request");
			var corpmodel = new CorporateUserModel
			{
				UserId = cusauth.Id.ToString(),
				Username = cusauth.Username,
				FullName = cusauth.FirstName + " " + cusauth.LastName,
				Email = cusauth.Email,
				Phone1 = cusauth.Phone
			};
			var newAccessToken = AuthService.JWTAuthentication(corpmodel);
			var newRefreshToken = AuthService.GenerateRefreshToken();
			user.TokenCode = newAccessToken.Token;
			user.RefreshToken = newRefreshToken;
			UnitOfWork.TokenBlackRepo.UpdateTokenBlack(user);
			UnitOfWork.Complete();

			return Ok(new LoginResponsedata
			{
				Responsecode = ResponseCode.SUCCESS,
				ResponseDescription = Message.Success,
				UserId = cusauth.Id,
				CustomerIdentity = cusauth.Username,
				Phone = cusauth.Phone,
				SecurityQuestion = "",
				access_token = newAccessToken.Token.Trim(),
				refresh_token = newRefreshToken.Trim(),
				//IndemnitySigned = isIndemnitySigned, 
				RegStage = cusauth.RegStage,
				Status = cusauth.Status,
				RoleId = cusauth.UserRoles
			});
		}
		[HttpPost("revoke")]
		public IActionResult Revoke()
		{
			if (!IsAuthenticated)
			{
				return StatusCode(401, "User is not authenticated");
			}
			var userId = User.Identity.Name;
			var user = UnitOfWork.TokenBlackRepo.GetTokenByUserId(Guid.Parse(userId));
			if (user == null) return BadRequest();
			user.RefreshTokenExpiryTime = null;
			user.IsBlack = 1;
			user.TokenCode = null;
			user.RefreshToken = null;
			UnitOfWork.TokenBlackRepo.UpdateTokenBlack(user);
			UnitOfWork.Complete();
			return NoContent();
		}
	}
}