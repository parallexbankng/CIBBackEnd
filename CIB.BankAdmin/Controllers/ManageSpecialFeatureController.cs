using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AutoMapper;
using CIB.Core.Common;
using CIB.Core.Common.Dto;
using CIB.Core.Common.Interface;
using CIB.Core.Common.Response;
using CIB.Core.Enums;
using CIB.Core.Services.Api;
using CIB.Core.Services.Authentication;
using CIB.Core.Services.Email;
using CIB.Core.Services.File;
using CIB.Core.Templates;
using CIB.Core.Utils;
using DocumentFormat.OpenXml.Vml.Office;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CIB.BankAdmin.Controllers
{
	[ApiController]
	[Route("api/BankAdmin/v1/[controller]")]
	public class ManageSpecialFeatureController : BaseAPIController
	{
		private readonly ILogger<ManageSpecialFeatureController> _logger;
		private readonly IApiService _apiService;
		private readonly IFileService _fileService;
		private readonly IEmailService _emailService;
		private readonly IConfiguration _config;
		public ManageSpecialFeatureController(IConfiguration config, IEmailService emailService, IFileService fileService, ILogger<ManageSpecialFeatureController> _logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IApiService apiService, IAuthenticationService authService) : base(mapper, unitOfWork, accessor, authService)
		{
			this._apiService = apiService;
			this._logger = _logger;
			this._fileService = fileService;
			this._emailService = emailService;
			this._config = config;

		}

		[HttpGet("GetSpecialFeatures")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public ActionResult<ListResponseDTO<AuthorizationTypeModel>> GetSpecialFeatures()
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

				var speacialFeature = new List<AuthorizationTypeModel>();
				var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
				foreach (var e in enums)
				{
					speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
				}
				return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data: speacialFeature, success: true, _message: Message.Success));
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}
		[HttpGet("SendEmail")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public ActionResult<ListResponseDTO<AuthorizationTypeModel>> SendEmail(string email)
		{
			try
			{
				ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.ResetPasswordCredentialMail(email, "ok", "ok")));
				return Ok(true);
			}
			catch (Exception ex)
			{
				_logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
				return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
			}
		}

		// [HttpPost("AddSpecialFeature")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// public ActionResult<ListResponseDTO<AuthorizationTypeModel>> AddSpecialFeatureToCustomer(GenericRequestDto model)
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         if(string.IsNullOrEmpty(model.Data))
		//         {
		//             return BadRequest("invalid request");
		//         }
		//         var itme = Encryption.DecryptStrings(model.Data);
		//         var requestData = JsonConvert.DeserializeObject<SpecialFeatureDto>(itme);
		//         if(requestData == null)
		//         {
		//             return BadRequest("invalid request data");
		//         }
		//         var payload = new SpecialFeatureDto
		//         {
		//             CorporateCustomerId = requestData.CorporateCustomerId,
		//             SpecialFeature =  requestData.Feature,
		//             ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
		//             IPAddress = Encryption.DecryptStrings(model.IPAddress),
		//             MACAddress = Encryption.DecryptStrings(model.MACAddress),
		//             HostName = Encryption.DecryptStrings(model.HostName)
		//         };
		//         // validation
		//         var mapTempProfile = Mapper.Map<TblSpecialFeature>(payload);
		//         mapTempProfile.Sn = 0;
		//         mapTempProfile.Id = Guid.NewGuid();
		//         mapTempProfile.IsTreated = (int) ProfileStatus.Pending;
		//         mapTempProfile.Status = (int) ProfileStatus.Modified;
		//         mapTempProfile.InitiatorId = BankProfile.Id;
		//         mapTempProfile.InitiatorUsername = UserName;
		//         mapTempProfile.DateRequested = DateTime.Now;
		//         mapTempProfile.Action = nameof(TempTableAction.Add_OnLending_Feature).Replace("_", " ");
		//         mapTempProfile.UserRoles = payload.UserRoleId;



		//         // var speacialFeature = new List<AuthorizationTypeModel>();
		//         // var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
		//         // foreach (var e in enums)
		//         // {
		//         //     speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
		//         // }
		//         // return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:speacialFeature,success:true, _message:Message.Success) );
		//         return Ok(true);
		//     }
		//     catch (Exception ex)
		//     {
		//          _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }

		//     //tblSpecialFeature: {id,sn,corporateCustomerId,speacialFeature,dateCreate,initBy,initusername,approId,approusename, dateapprove, status,action,}
		// }

		// [HttpGet("GetPendingSpecialFeature")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// public ActionResult<ListResponseDTO<AuthorizationTypeModel>> GetPendingSpecialFeature()
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         var speacialFeature = new List<AuthorizationTypeModel>();
		//         var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
		//         foreach (var e in enums)
		//         {
		//             speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
		//         }
		//         return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:speacialFeature,success:true, _message:Message.Success) );
		//     }
		//     catch (Exception ex)
		//     {
		//          _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }
		// }

		// [HttpGet("GetApprovedSpecialFeature")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// public ActionResult<ListResponseDTO<AuthorizationTypeModel>> GetApprovedSpecialFeature()
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         var speacialFeature = new List<AuthorizationTypeModel>();
		//         var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
		//         foreach (var e in enums)
		//         {
		//             speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
		//         }
		//         return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:speacialFeature,success:true, _message:Message.Success) );
		//     }
		//     catch (Exception ex)
		//     {
		//          _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }
		// }

		// [HttpPost("ApprovedSpecialFeature")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// public ActionResult<ListResponseDTO<AuthorizationTypeModel>> ApprovedSpecialFeature(GenericRequestDto model)
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         if(string.IsNullOrEmpty(model.Data))
		//         {
		//             return BadRequest("invalid request");
		//         }
		//         var itme = Encryption.DecryptStrings(model.Data);
		//         var requestData = JsonConvert.DeserializeObject<SpecialFeatureDto>(itme);
		//         if(requestData == null)
		//         {
		//             return BadRequest("invalid request data");
		//         }
		//         var payload = new SpecialFeatureDto
		//         {
		//             CorporateCustomerId = requestData.CorporateCustomerId,
		//             SpecialFeature =  requestData.Feature,
		//             ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
		//             IPAddress = Encryption.DecryptStrings(model.IPAddress),
		//             MACAddress = Encryption.DecryptStrings(model.MACAddress),
		//             HostName = Encryption.DecryptStrings(model.HostName)
		//         };
		//         // validation
		//         var mapTempProfile = Mapper.Map<TblSpecialFeature>(payload);
		//         mapTempProfile.Sn = 0;
		//         mapTempProfile.Id = Guid.NewGuid();
		//         mapTempProfile.IsTreated = (int) ProfileStatus.Pending;
		//         mapTempProfile.Status = (int) ProfileStatus.Modified;
		//         mapTempProfile.InitiatorId = BankProfile.Id;
		//         mapTempProfile.InitiatorUsername = UserName;
		//         mapTempProfile.DateRequested = DateTime.Now;
		//         mapTempProfile.Action = nameof(TempTableAction.Add_OnLending_Feature).Replace("_", " ");
		//         mapTempProfile.UserRoles = payload.UserRoleId;



		//         // var speacialFeature = new List<AuthorizationTypeModel>();
		//         // var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
		//         // foreach (var e in enums)
		//         // {
		//         //     speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
		//         // }
		//         // return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:speacialFeature,success:true, _message:Message.Success) );
		//         return Ok(true);
		//     }
		//     catch (Exception ex)
		//     {
		//          _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }

		//     //tblSpecialFeature: {id,sn,corporateCustomerId,speacialFeature,dateCreate,initBy,initusername,approId,approusename, dateapprove, status,action,}
		// }

		// [HttpPost("DeclineSpecialFeature")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// public ActionResult<ListResponseDTO<AuthorizationTypeModel>> DeclineSpecialFeature(GenericRequestDto model)
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         if(string.IsNullOrEmpty(model.Data))
		//         {
		//             return BadRequest("invalid request");
		//         }
		//         var itme = Encryption.DecryptStrings(model.Data);
		//         var requestData = JsonConvert.DeserializeObject<SpecialFeatureDto>(itme);
		//         if(requestData == null)
		//         {
		//             return BadRequest("invalid request data");
		//         }
		//         var payload = new SpecialFeatureDto
		//         {
		//             CorporateCustomerId = requestData.CorporateCustomerId,
		//             SpecialFeature =  requestData.Feature,
		//             ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
		//             IPAddress = Encryption.DecryptStrings(model.IPAddress),
		//             MACAddress = Encryption.DecryptStrings(model.MACAddress),
		//             HostName = Encryption.DecryptStrings(model.HostName)
		//         };
		//         // validation
		//         var mapTempProfile = Mapper.Map<TblSpecialFeature>(payload);
		//         mapTempProfile.Sn = 0;
		//         mapTempProfile.Id = Guid.NewGuid();
		//         mapTempProfile.IsTreated = (int) ProfileStatus.Pending;
		//         mapTempProfile.Status = (int) ProfileStatus.Modified;
		//         mapTempProfile.InitiatorId = BankProfile.Id;
		//         mapTempProfile.InitiatorUsername = UserName;
		//         mapTempProfile.DateRequested = DateTime.Now;
		//         mapTempProfile.Action = nameof(TempTableAction.Add_OnLending_Feature).Replace("_", " ");
		//         mapTempProfile.UserRoles = payload.UserRoleId;



		//         // var speacialFeature = new List<AuthorizationTypeModel>();
		//         // var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
		//         // foreach (var e in enums)
		//         // {
		//         //     speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
		//         // }
		//         // return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:speacialFeature,success:true, _message:Message.Success) );
		//         return Ok(true);
		//     }
		//     catch (Exception ex)
		//     {
		//          _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }

		//     //tblSpecialFeature: {id,sn,corporateCustomerId,speacialFeature,dateCreate,initBy,initusername,approId,approusename, dateapprove, status,action,}
		// }

		// [HttpPost("RequestSpecialFeatureApproval")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// public ActionResult<ListResponseDTO<AuthorizationTypeModel>> RequestSpecialFeatureApproval(GenericRequestDto model)
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         if(string.IsNullOrEmpty(model.Data))
		//         {
		//             return BadRequest("invalid request");
		//         }
		//         var itme = Encryption.DecryptStrings(model.Data);
		//         var requestData = JsonConvert.DeserializeObject<SpecialFeatureDto>(itme);
		//         if(requestData == null)
		//         {
		//             return BadRequest("invalid request data");
		//         }
		//         var payload = new SpecialFeatureDto
		//         {
		//             CorporateCustomerId = requestData.CorporateCustomerId,
		//             SpecialFeature =  requestData.Feature,
		//             ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
		//             IPAddress = Encryption.DecryptStrings(model.IPAddress),
		//             MACAddress = Encryption.DecryptStrings(model.MACAddress),
		//             HostName = Encryption.DecryptStrings(model.HostName)
		//         };
		//         // validation
		//         var mapTempProfile = Mapper.Map<TblSpecialFeature>(payload);
		//         mapTempProfile.Sn = 0;
		//         mapTempProfile.Id = Guid.NewGuid();
		//         mapTempProfile.IsTreated = (int) ProfileStatus.Pending;
		//         mapTempProfile.Status = (int) ProfileStatus.Modified;
		//         mapTempProfile.InitiatorId = BankProfile.Id;
		//         mapTempProfile.InitiatorUsername = UserName;
		//         mapTempProfile.DateRequested = DateTime.Now;
		//         mapTempProfile.Action = nameof(TempTableAction.Add_OnLending_Feature).Replace("_", " ");
		//         mapTempProfile.UserRoles = payload.UserRoleId;



		//         // var speacialFeature = new List<AuthorizationTypeModel>();
		//         // var enums = Enum.GetValues(typeof(Core.Enums.SpecialFeature)).Cast<SpecialFeature>().ToList();
		//         // foreach (var e in enums)
		//         // {
		//         //     speacialFeature.Add(new AuthorizationTypeModel { Key = e.ToString(), Name = e.ToString().Replace("_", " ") });
		//         // }
		//         // return Ok(new ListResponseDTO<AuthorizationTypeModel>(_data:speacialFeature,success:true, _message:Message.Success) );
		//         return Ok(true);
		//     }
		//     catch (Exception ex)
		//     {
		//          _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }

		//     //tblSpecialFeature: {id,sn,corporateCustomerId,speacialFeature,dateCreate,initBy,initusername,approId,approusename, dateapprove, status,action,}
		// }

		// [HttpPost("BulkRequestDecline")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// [ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
		// public ActionResult<bool> BulkRequestDecline(GenericRequestDto model)
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         if(string.IsNullOrEmpty(model.Data))
		//         {
		//             return BadRequest("invalid request");
		//         }

		//         var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
		//         if(requestData == null)
		//         {
		//             return BadRequest("invalid request data");
		//         }
		//         var responseErrors = new List<BulkError>();
		//         foreach(var item in requestData)
		//         {
		//             var payload = new AppActionDto
		//             {
		//                 Id = item.Id,
		//                 Reason = item.Reason,
		//                 IPAddress = Encryption.DecryptStrings(model.IPAddress),
		//                 ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
		//                 HostName = Encryption.DecryptStrings(model.HostName)
		//             };
		//             var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
		//             if (entity == null)
		//             {
		//                 var bulkError = new BulkError
		//                 {
		//                     Message = "Invalid Id. Customer does not exist",
		//                     ActionInfo = $"CorporateCustomerID : {payload.Id}"
		//                 };
		//                 responseErrors.Add(bulkError);
		//             }
		//             else
		//             {
		//                 if (entity.Status == (int)ProfileStatus.Active)
		//                 {
		//                     var bulkError = new BulkError
		//                     {
		//                         Message = "Customer is already approved",
		//                         ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
		//                     };
		//                     responseErrors.Add(bulkError);
		//                 }
		//                 else
		//                 {
		//                      if(!DeclineRequest(entity,payload,out string errorMessage ))
		//                     {
		//                         var bulkError = new BulkError
		//                         {
		//                             Message = errorMessage,
		//                             ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
		//                         };
		//                         responseErrors.Add(bulkError);
		//                     }
		//                 }
		//             }
		//         }
		//         if(responseErrors.Any())
		//         {
		//             return BadRequest(responseErrors);
		//         }
		//         return Ok(true);
		//     }
		//     catch (Exception ex)
		//     {

		//         _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }
		// }

		// [HttpPost("BulkRequestApproval")]
		// [ProducesResponseType(StatusCodes.Status200OK)]
		// [ProducesResponseType(typeof(List<BulkError>), StatusCodes.Status400BadRequest)]
		// public ActionResult<bool> BulkRequestApproval(GenericRequestDto model)
		// {
		//     try
		//     {
		//         if (!IsAuthenticated)
		//         {
		//             return StatusCode(401, "User is not authenticated");
		//         }

		//         string errormsg = string.Empty;

		//         if (!IsUserActive(out errormsg))
		//         {
		//             return StatusCode(400, errormsg);
		//         }

		//         if(string.IsNullOrEmpty(model.Data))
		//         {
		//             return BadRequest("invalid request");
		//         }

		//         var requestData = JsonConvert.DeserializeObject<List<SimpleAction>>(Encryption.DecryptStrings(model.Data));
		//         if(requestData == null)
		//         {
		//             return BadRequest("invalid request data");
		//         }

		//         var responseErrors = new List<BulkError>();
		//         foreach(var item in requestData)
		//         {
		//             var payload = new SimpleAction
		//             {
		//                 Id = item.Id,
		//                 IPAddress = Encryption.DecryptStrings(model.IPAddress),
		//                 HostName = Encryption.DecryptStrings(model.HostName),
		//                 ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
		//                 MACAddress = Encryption.DecryptStrings(model.MACAddress)
		//             };
		//             var entity = UnitOfWork.TemCorporateCustomerRepo.GetByIdAsync(payload.Id);
		//             if (entity == null)
		//             {
		//                 var bulkError = new BulkError
		//                 {
		//                     Message = "Invalid Id. Customer does not exist",
		//                     ActionInfo = $"UserName : {payload.Id}, Action: {entity.Action}"
		//                 };
		//                 responseErrors.Add(bulkError);
		//             }
		//             else
		//             {
		//                 if(entity.Status == 1)
		//                 {
		//                     var bulkError = new BulkError
		//                     {
		//                         Message = "Profile was not declined or modified initially",
		//                         ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
		//                     };
		//                     responseErrors.Add(bulkError);
		//                 }
		//                 else
		//                 {

		//                     if(entity.InitiatorId != BankProfile.Id)
		//                     {
		//                         var bulkError = new BulkError
		//                         {
		//                             Message = "This Request Was not Initiated By you",
		//                             ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
		//                         };
		//                         responseErrors.Add(bulkError);
		//                     }
		//                     else
		//                     {
		//                         // if(!RequestApproval(entity, payload, out string errorMessage))
		//                         // {
		//                         //     var bulkError = new BulkError
		//                         //     {
		//                         //         Message = errorMessage,
		//                         //        ActionInfo = $"CompanyName : {entity.CompanyName}, CustomerId : {entity.CustomerId},Action: {entity.Action}"
		//                         //     };
		//                         //     responseErrors.Add(bulkError);
		//                         // }
		//                     }
		//                 }

		//             }
		//         }
		//         if(responseErrors.Any())
		//         {
		//             return BadRequest(responseErrors);
		//         }
		//         return Ok(true);
		//     }
		//     catch (Exception ex)
		//     {

		//         _logger.LogError("SERVER ERROR {0}, {1}, {2}",Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
		//         return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus:false)) : StatusCode(500, new ErrorResponse(responsecode:ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus:false));
		//     }
		// }

		// private bool ApprovedRequest(TblTempCorporateCustomer profile, SimpleAction payload, out string errorMessage)
		// {

		//     if(profile.Action == nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
		//     {

		//         var entity = Mapper.Map<TblCorporateCustomer>(profile);
		//         var userStatus = UnitOfWork.CorporateCustomerRepo.CheckDuplicate(entity);
		//         if(userStatus.IsDuplicate != "02")
		//         {
		//             errorMessage = userStatus.Message;
		//             return false;
		//         }

		//         if (entity.Status == (int) ProfileStatus.Active)
		//         {
		//             errorMessage = "Profile is already active";
		//             return false;
		//         } 

		//         var mapProfile = new TblCorporateProfile
		//         {
		//             Id = Guid.NewGuid(),
		//             CorporateCustomerId = entity.Id,
		//             Username = profile.UserName,
		//             Phone1 = profile.Phone1,
		//             Email = profile.Email1,
		//             FirstName = profile.FirstName,
		//             MiddleName = profile.MiddleName,
		//             ApprovalLimit = profile.ApprovalLimit,
		//             LastName = profile.LastName,
		//             Password = Encryption.EncriptPassword(PasswordValidator.GeneratePassword()),
		//             FullName = profile.FullName,
		//             Status = (int)ProfileStatus.Active,
		//             RegStage = 0,
		//             DateCompleted = DateTime.Now
		//         };

		//         if (Enum.TryParse(entity.AuthorizationType.Replace(" ", "_"), out AuthorizationType _authType))
		//         {
		//             if (_authType == AuthorizationType.Single_Signatory)
		//             {

		//             }
		//             else
		//             {
		//                 mapProfile.CorporateRole = profile.CorporateRoleId;
		//             }
		//         }

		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =   $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Active)}",
		//             PreviousFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
		//             TransactionId = "",
		//             UserId = Guid.Parse(UserRoleId),
		//             Username = UserName,
		//             Description = " Approved Corporate Customer Account",
		//             TimeStamp = DateTime.Now
		//         };

		//        // UnitOfWork.Complete();

		//         entity.Status = (int)ProfileStatus.Active;
		//         profile.IsTreated = (int)ProfileStatus.Active;
		//         entity.DateAdded = DateTime.Now;
		//         profile.CorporateCustomerId = mapProfile.Id;
		//         entity.Sn = 0;
		//         mapProfile.RegStage = 0;
		//         UnitOfWork.CorporateCustomerRepo.Add(entity);
		//         UnitOfWork.CorporateProfileRepo.Add(mapProfile);
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         var password = Encryption.DecriptPassword(mapProfile.Password);
		//         var authUrl = _config.GetValue<string>("authUrl:coporate");
		//         ThreadPool.QueueUserWorkItem(_ => _emailService.SendEmail(EmailTemplate.LoginCredentialMail(mapProfile.Email, mapProfile.FullName, mapProfile.Username, password, entity.CustomerId,authUrl)));

		//         errorMessage = "";
		//         return true;
		//     }

		//     if(profile.Action == nameof(TempTableAction.Update).Replace("_", " "))
		//     {
		//         var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
		//         if(entity == null)
		//         {
		//             errorMessage = "Invalid Corporate Customer Id";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =  $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
		//             $"Authorization Type: {profile.AuthorizationType.Replace("_"," ")}, Default Account Name: {profile.DefaultAccountName}, " +
		//             $"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_"," ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Approved Bank Profile Update. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };

		//         entity.CompanyName = profile.CompanyName;
		//         entity.CustomerId = profile.CustomerId;
		//         entity.Email1 = profile.Email1;
		//         entity.DefaultAccountName = profile.DefaultAccountName;
		//         entity.DefaultAccountNumber = profile.DefaultAccountNumber;
		//         entity.AuthorizationType = profile.AuthorizationType;

		//         var userStatus = UnitOfWork.CorporateCustomerRepo.CheckDuplicate(entity, true);
		//         if(userStatus.IsDuplicate != "02")
		//         {
		//             errorMessage = userStatus.Message;
		//             return false;
		//         }
		//         var originalStatus = entity.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Active;
		//         profile.IsTreated = (int) ProfileStatus.Active;
		//         entity.Status = originalStatus;
		//         profile.ApprovedId = BankProfile.Id;
		//         profile.ApprovalUsername = UserName;
		//         profile.ActionResponseDate = DateTime.Now;
		//         profile.Reasons = payload.Reason;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         errorMessage = "";
		//         return true;
		//     }

		//     if(profile.Action == nameof(TempTableAction.Change_Account_Signatory).Replace("_", " "))
		//     {
		//         var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
		//         if(entity == null)
		//         {
		//             errorMessage = "Invalid Corporate Customer Id";
		//             return false;
		//         }
		//         var profileEntity = UnitOfWork.CorporateProfileRepo.GetCorporateProfiles(entity.Id);
		//         if(!profileEntity.Any())
		//         {
		//             errorMessage = "no corporate profile is associated to this  Corporate Customer";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =  $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
		//             $"Authorization Type: {profile.AuthorizationType.Replace("_"," ")}, Default Account Name: {profile.DefaultAccountName}, " +
		//             $"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_"," ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Approved Change of Corporate Customer Signatory. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };


		//         var updatedProfile = profileEntity.FirstOrDefault();
		//         entity.AuthorizationType = profile.AuthorizationType;
		//         var originalStatus = entity.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Active;
		//         var originalProfileStatus = updatedProfile.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Active;

		//         profile.IsTreated = (int) ProfileStatus.Active;
		//         updatedProfile.Status = originalProfileStatus;
		//         updatedProfile.CorporateRole = profile.CorporateRoleId;
		//         entity.Status = originalStatus;
		//         profile.ApprovedId = BankProfile.Id;
		//         profile.ApprovalUsername = UserName;
		//         profile.ActionResponseDate = DateTime.Now;
		//         profile.Reasons = payload.Reason;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
		//         UnitOfWork.CorporateProfileRepo.UpdateCorporateProfile(updatedProfile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         errorMessage = "";
		//         return true;
		//     }

		//     if(profile.Action == nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
		//     {    
		//        var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
		//         if(entity == null)
		//         {
		//             errorMessage = "Invalid Corporate Customer Id";
		//             return false;
		//         }

		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {nameof(ProfileStatus.Active)}",
		//             PreviousFieldValue =  "",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Approved Bank Profile Role Update. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };

		//         entity.MinAccountLimit = profile.MinAccountLimit;
		//         entity.MaxAccountLimit = profile.MaxAccountLimit;
		//         entity.SingleTransDailyLimit = profile.SingleTransDailyLimit;
		//         entity.BulkTransDailyLimit = profile.BulkTransDailyLimit;

		//         var originalStatus = entity.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Active;
		//         entity.Status = originalStatus;
		//         profile.IsTreated = (int) ProfileStatus.Active;
		//         profile.ApprovedId = BankProfile.Id;
		//         profile.ApprovalUsername = UserName;
		//         profile.ActionResponseDate = DateTime.Now;
		//         profile.Reasons = payload.Reason;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         errorMessage = "";
		//         return true;
		//     }

		//     if(profile.Action == nameof(TempTableAction.Deactivate).Replace("_", " "))
		//     {    
		//         var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
		//         if(entity == null)
		//         {
		//             errorMessage = "Invalid Corporate Customer Id";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
		//             PreviousFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Approved Bank Profile Deactivation. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };

		//         entity.Status = (int) ProfileStatus.Deactivated;
		//         profile.Status = (int) ProfileStatus.Deactivated;
		//         profile.IsTreated = (int) ProfileStatus.Active;
		//         //entity.ReasonsForDeactivation = profile.Reasons;
		//         profile.ApprovedId = BankProfile.Id;
		//         profile.ApprovalUsername = UserName;
		//         profile.ActionResponseDate = DateTime.Now;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         errorMessage = "";
		//         return true;
		//     }

		//     if(profile.Action == nameof(TempTableAction.Reactivate).Replace("_", " "))
		//     {    
		//         var entity = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)profile.CorporateCustomerId);
		//         if(entity == null)
		//         {
		//             errorMessage = "Invalid Corporate Customer Id";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Approve).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
		//             PreviousFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Approved Bank Profile Reactivation. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };

		//         entity.Status = (int) ProfileStatus.Active;
		//         profile.Status = (int)ProfileStatus.Active;
		//         profile.IsTreated = (int)ProfileStatus.Active;
		//         profile.ApprovedId = BankProfile.Id;
		//         profile.ApprovalUsername = UserName;
		//         profile.ActionResponseDate = DateTime.Now;
		//         profile.Reasons = "";
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(profile);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(entity);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         errorMessage = "";
		//         return true;
		//     }

		//     errorMessage = "Unknow Request";
		//     return false;
		// }
		// private bool RequestApproval(TblTempCorporateCustomer entity, SimpleAction payload, out string errorMessage)
		// {
		//     var emailNotification = new EmailNotification
		//     {
		//         CompanyName = entity.CompanyName,
		//         CustomerId = entity.CustomerId,
		//         Action = entity.Action,
		//         MinAccountLimit = entity.MinAccountLimit,
		//         MaxAccountLimit = entity.MaxAccountLimit,
		//         SingleTransDailyLimit =entity.SingleTransDailyLimit,
		//         BulkTransDailyLimit = entity.BulkTransDailyLimit,
		//         ApprovalLimit = entity.ApprovalLimit
		//     };

		//     if(entity.Action ==  nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
		//     {
		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//             errorMessage = "Profile wasn't Decline or modified initially";
		//             return false;
		//         }

		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             NewFieldValue =  $"Company Name: {entity.CompanyName}, Company Email: {entity.Email1}, CustomerId: {entity.CustomerId}, Account Number: {entity.DefaultAccountNumber}, Account Name: {entity.DefaultAccountName}, AuthorizationType: {entity.AuthorizationType}, Phone Number: {entity.PhoneNumber}",
		//             PreviousFieldValue = "",
		//             TransactionId = "",
		//             UserId = Guid.Parse(UserRoleId),
		//             Username = UserName,
		//             Description = "Create Corporate Customer by bank admin",
		//             TimeStamp = DateTime.Now
		//         };

		//         //email notification
		//         entity.Status = (int) ProfileStatus.Pending;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity,emailNotification);
		//         errorMessage = "Request Approval Was Successful";
		//         return true;
		//     }

		//      if(entity.Action ==  nameof(TempTableAction.Change_Account_Signatory).Replace("_", " "))
		//     {

		//         var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
		//         if (profile == null)
		//         {
		//             errorMessage = "Invalid Bank Profile id";
		//             return false;
		//         }

		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//             errorMessage = "Profile wasn't Decline or modified initially";
		//             return false;
		//         }

		//         if(entity.Status == (int)ProfileStatus.Pending)
		//         {
		//             //errorMessage = "Profile wasn't Decline or modified initially";
		//             errorMessage ="There is a pending request awaiting Approval";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_"," ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue =  $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
		//             $"Authorization Type: {profile.AuthorizationType.Replace("_", " ")}, Default Account Name: {profile.DefaultAccountName}, " +
		//             $"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {status}",
		//             TransactionId = "",
		//             UserId = Guid.Parse(UserRoleId),
		//             Username = UserName,
		//             Description = "Modified Corporate Customer Info By Bank Admin",
		//             TimeStamp = DateTime.Now
		//         };

		//         //update status

		//          var changeNotification = new EmailNotification
		//         {
		//             CompanyName = entity.CompanyName,
		//             CustomerId = entity.CustomerId,
		//             Action = entity.Action,
		//             MinAccountLimit = entity.MinAccountLimit,
		//             MaxAccountLimit = entity.MaxAccountLimit,
		//             SingleTransDailyLimit =entity.SingleTransDailyLimit,
		//             BulkTransDailyLimit = entity.BulkTransDailyLimit,
		//             ApprovalLimit = entity.ApprovalLimit
		//         };

		//         entity.Status = (int) ProfileStatus.Pending;
		//         profile.Status = (int) ProfileStatus.Pending;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity,changeNotification);
		//         errorMessage = "Request Approval Was Successful";
		//         return true;
		//     }

		//     if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
		//     {
		//         // var tblRole = UnitOfWork.CorporateRoleRepo.GetByIdAsync(Guid.Parse(entity.CorporateRole));
		//         var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
		//         if (profile == null)
		//         {
		//             errorMessage = "Invalid Bank Profile id";
		//             return false;
		//         }

		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//             errorMessage = "Profile wasn't Decline or modified initially";
		//             return false;
		//         }

		//         if(entity.Status == (int)ProfileStatus.Pending)
		//         {
		//             //errorMessage = "Profile wasn't Decline or modified initially";
		//             errorMessage ="There is a pending request awaiting Approval";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Request_Approval).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_"," ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue =  $"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
		//             $"Authorization Type: {profile.AuthorizationType.Replace("_", " ")}, Default Account Name: {profile.DefaultAccountName}, " +
		//             $"Default Account Number: {profile.DefaultAccountNumber}, Email: {profile.Email1}, Status: {status}",
		//             TransactionId = "",
		//             UserId = Guid.Parse(UserRoleId),
		//             Username = UserName,
		//             Description = "Modified Corporate Customer Info By Bank Admin",
		//             TimeStamp = DateTime.Now
		//         };

		//         //update status

		//         entity.Status = (int) ProfileStatus.Pending;
		//         profile.Status = (int) ProfileStatus.Pending;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity,emailNotification);
		//         errorMessage = "Request Approval Was Successful";
		//         return true;
		//     }

		//     if(entity.Action ==  nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
		//     {
		//         var profile  = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);

		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//             errorMessage = "Profile wasn't Decline or modified initially";
		//             return false;
		//         }

		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Update).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue =$"Company Name: {profile.CompanyName}, Customer ID: {profile.CustomerId}, " +
		//             $"Maximum Account Limit: {profile.MaxAccountLimit}, Minimum Account Limit: {profile.MinAccountLimit}, Status: {status}",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = "Updated Account Limit of Corporate Customer",
		//             TimeStamp = DateTime.Now
		//         };

		//         //update status
		//         var originalStatus = profile.Status == (int) ProfileStatus.Deactivated ? (int) ProfileStatus.Deactivated : (int)ProfileStatus.Pending;

		//         entity.Status = (int) ProfileStatus.Pending;
		//         profile.Status = originalStatus;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         //notify.NotifyBankAuthorizerForCorporate(entity.Action,null,null,entity,null);
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerApproval(entity,emailNotification);
		//         errorMessage = "Request Approval Was Successful";
		//         return true;
		//     }

		//     errorMessage = "invalid Request";
		//     return false;
		// }
		// private bool DeclineRequest(TblTempCorporateCustomer entity, AppActionDto payload, out string errorMessage)
		// {
		//     var initiatorProfile = UnitOfWork.BankProfileRepo.GetByIdAsync((Guid)entity.InitiatorId);

		//     var emailNotification = new EmailNotification
		//     {
		//         CustomerId = entity.CustomerId,
		//         Email = entity.CorporateEmail,
		//         AccountName = entity.DefaultAccountName,
		//         AccountNumber = entity.DefaultAccountNumber,
		//         Action = entity.Action,
		//         MinAccountLimit = entity.MinAccountLimit,
		//         MaxAccountLimit = entity.MaxAccountLimit,
		//         SingleTransDailyLimit =entity.SingleTransDailyLimit,
		//         BulkTransDailyLimit = entity.BulkTransDailyLimit,
		//         ApprovalLimit = entity.ApprovalLimit,
		//         Reason = payload.Reason
		//     };

		//     if(entity.Action ==  nameof(TempTableAction.Onboard_Corporate_Customer).Replace("_", " "))
		//     {
		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//             errorMessage = "Profile wasn't Decline or modified initially";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//               NewFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_"," ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue = "",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Decline Approval for new Bank Profile. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };


		//         //update status
		//         entity.Status = (int) ProfileStatus.Declined;
		//         entity.IsTreated = (int) ProfileStatus.Declined;
		//         entity.Reasons = payload.Reason;
		//         entity.ApprovedId = BankProfile.Id;
		//         entity.ApprovalUsername = UserName;
		//         entity.ActionResponseDate = DateTime.Now;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile,emailNotification);
		//         errorMessage = "Decline Approval Was Successful";
		//         return true;
		//     }

		//     if(entity.Action ==  nameof(TempTableAction.Update).Replace("_", " "))
		//     {

		//         var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
		//         if (profile == null)
		//         {
		//             errorMessage = "Invalid Bank Profile id";
		//             return false;
		//         }

		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//             errorMessage = "Profile wasn't Decline or modified initially";
		//             return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//             Id = Guid.NewGuid(),
		//             ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
		//             Ipaddress = payload.IPAddress,
		//             Macaddress = payload.MACAddress,
		//             HostName = payload.HostName,
		//             ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//             NewFieldValue =  $"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//             $"Authorization Type: {entity.AuthorizationType.Replace("_"," ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//             $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Modified)}",
		//             PreviousFieldValue = "",
		//             TransactionId = "",
		//             UserId = BankProfile.Id,
		//             Username = UserName,
		//             Description = $"Decline Approval to Update Corporate Customer Information. Action was carried out by a Bank user",
		//             TimeStamp = DateTime.Now
		//         };

		//         entity.Status = (int)ProfileStatus.Declined;
		//         profile.Status = (int)entity.PreviousStatus;
		//         entity.IsTreated = (int)ProfileStatus.Declined;
		//         entity.Reasons = payload.Reason;
		//         entity.ApprovedId = BankProfile.Id;
		//         entity.ApprovalUsername = UserName;
		//         entity.ActionResponseDate = DateTime.Now;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();

		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile,emailNotification);
		//         errorMessage = "Decline Approval Was Successful";
		//         return true;
		//     }

		//     if(entity.Action ==  nameof(TempTableAction.Update_Account_limit).Replace("_", " "))
		//     {
		//       if (entity.CorporateCustomerId != null)
		//       {
		//         var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
		//         if (profile == null)
		//         {
		//           errorMessage = "Invalid Corporate Customer id";
		//           return false;
		//         }

		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//           errorMessage = "Profile wasn't Decline or modified initially";
		//           return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//           Id = Guid.NewGuid(),
		//           ActionCarriedOut = nameof(AuditTrailAction.Decline).Replace("_", " "),
		//           Ipaddress = payload.IPAddress,
		//           Macaddress = payload.MACAddress,
		//           HostName = payload.HostName,
		//           ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//           NewFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//                          $"Maximum Account Limit: {entity.MaxAccountLimit}, Minimum Account Limit: {entity.MinAccountLimit}, Status: {nameof(ProfileStatus.Modified)}",

		//           PreviousFieldValue = "",
		//           TransactionId = "",
		//           UserId = BankProfile.Id,
		//           Username = UserName,
		//           Description = $"Decline Request for Bank Profile Role Change. Action was carried out by a Bank user",
		//           TimeStamp = DateTime.Now
		//         };

		//         //update status
		//         //notify.NotifyBankAdminAuthorizerForCorporate(entity,true, payload.Reason);
		//         entity.Status = (int) ProfileStatus.Declined;
		//         profile.Status = (int) entity.PreviousStatus;
		//         entity.IsTreated =(int) ProfileStatus.Declined;
		//         entity.Reasons = payload.Reason;
		//         entity.ApprovedId = BankProfile.Id;
		//         entity.ApprovalUsername = UserName;
		//         entity.ActionResponseDate = DateTime.Now;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile,emailNotification);
		//         errorMessage = "Request Decline Was Successful";
		//         return true;
		//       }
		//       errorMessage = "Invalid Corporate Customer id";
		//       return false;
		//     }

		//     if(entity.Action ==  nameof(TempTableAction.Reactivate).Replace("_", " "))
		//     {
		//       if (entity.CorporateCustomerId != null)
		//       {
		//         var profile = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)entity.CorporateCustomerId);
		//         if (profile == null)
		//         {
		//           errorMessage = "Invalid Corporate Customer Id";
		//           return false;
		//         }

		//         if (entity.Status != (int)ProfileStatus.Pending && entity.Status != (int)ProfileStatus.Declined && entity.Status != (int)ProfileStatus.Modified) 
		//         {
		//           errorMessage = "Profile wasn't Decline or modified initially";
		//           return false;
		//         }
		//         var status = (ProfileStatus)entity.Status;
		//         var auditTrail = new TblAuditTrail
		//         {
		//           Id = Guid.NewGuid(),
		//           ActionCarriedOut = nameof(AuditTrailAction.Reactivate).Replace("_", " "),
		//           Ipaddress = payload.IPAddress,
		//           Macaddress = payload.MACAddress,
		//           HostName = payload.HostName,
		//           ClientStaffIpaddress = payload.ClientStaffIPAddress,
		//           NewFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//                          $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//                          $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {nameof(ProfileStatus.Deactivated)}",
		//           PreviousFieldValue =$"Company Name: {entity.CompanyName}, Customer ID: {entity.CustomerId}, " +
		//                               $"Authorization Type: {entity.AuthorizationType.Replace("_", " ")}, Default Account Name: {entity.DefaultAccountName}, " +
		//                               $"Default Account Number: {entity.DefaultAccountNumber}, Email: {entity.Email1}, Status: {status}",
		//           TransactionId = "",
		//           UserId = BankProfile.Id,
		//           Username = UserName,
		//           Description = $"Decline Request for Bank Profile Reactivation. Action was carried out by a Bank user",
		//           TimeStamp = DateTime.Now
		//         };

		//         //update status
		//         entity.Status = (int) ProfileStatus.Declined;
		//         profile.Status = (int) entity.PreviousStatus;
		//         entity.IsTreated = (int) ProfileStatus.Declined;
		//         entity.Reasons = payload.Reason;
		//         entity.ApprovedId = BankProfile.Id;
		//         entity.ApprovalUsername = UserName;
		//         entity.ActionResponseDate = DateTime.Now;
		//         UnitOfWork.TemCorporateCustomerRepo.UpdateTemCorporateCustomer(entity);
		//         UnitOfWork.CorporateCustomerRepo.UpdateCorporateCustomer(profile);
		//         UnitOfWork.AuditTrialRepo.Add(auditTrail);
		//         UnitOfWork.Complete();
		//         notify.NotifyBankAdminAuthorizerForCorporateCustomerDecline(initiatorProfile,emailNotification);
		//         errorMessage = "Decline Request Was Successful";
		//         return true;
		//       }
		//       errorMessage = "Invalid Corporate Customer Id is Require";
		//       return false;
		//     }
		//     errorMessage = "invalid Request";
		//     return false;
		// }

	}
}