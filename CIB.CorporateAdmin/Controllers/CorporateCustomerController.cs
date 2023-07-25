using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Interface;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Services.Api;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using CIB.Core.Entities;
using CIB.Core.Common.Response;
using CIB.Core.Enums;
using Microsoft.Extensions.Logging;
using CIB.Core.Common.Dto;

namespace CIB.CorporateAdmin.Controllers
{
    [ApiController]
    [Route("api/CorporateAdmin/v1/[controller]")]
    public class CorporateCustomerController : BaseAPIController
    {
        private readonly IApiService _apiService;
        protected readonly IConfiguration _config;
        private readonly ILogger<BulkTransactionController> _logger;
        public CorporateCustomerController(ILogger<BulkTransactionController> logger, IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IApiService apiService, IConfiguration config) : base(unitOfWork, mapper, accessor)
        {
            this._apiService = apiService;
            this._config = config;
            this._logger = logger;
        }

        [HttpPost("AccountStatements")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<StatementOfAccountResponseDto>> AccountStatements(StatementOfAccount model)
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

                if (UnitOfWork.CorporateUserRoleAccessRepo.IsCorporateAdmin(UserRoleId))
                {
                    return BadRequest("UnAuthorized Access");
                }


                var payload = new StatementOfAccountRequestDto
                {
                    Channel = "2",
                    AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
                    Period = Encryption.DecryptStrings(model.Period),
                    DocumentType = Encryption.DecryptStrings(model.DocumentType),
                    StartDate = Encryption.DecryptStrings(model.StartDate),
                    EndDate = Encryption.DecryptStrings(model.EndDate),
                    SendToEmail = Encryption.DecryptBooleans(model.SendToEmail),
                    SendTo3rdPardy = Encryption.DecryptBooleans(model.SendTo3rdPardy),
                    RecipientEmail = Encryption.DecryptStrings(model.RecipientEmail),
                    TypeOfDestination = Encryption.DecryptStrings(model.TypeOfDestination),
                    DestinationCode = Encryption.DecryptStrings(model.DestinationCode),
                };

                //call statement of account API
                payload.StartDate ??= "";
                payload.EndDate ??= "";
                payload.DocumentType ??= "pdf";
                payload.RecipientEmail ??= "";
                payload.TypeOfDestination ??= "";
                payload.DestinationCode ??= "";
                var result = await _apiService.GenerateStatement(payload);
                if (result.ResponseCode != "00")
                {
                    return BadRequest(result.ResponseDescription);
                }

                if (payload.Period.Contains("Specify Period"))
                {
                    var acctInfod = await _apiService.GetCustomerDetailByAccountNumber(payload.AccountNumber);
                    if (acctInfod.ResponseCode != "00")
                    {
                        return BadRequest(acctInfod.ResponseDescription);
                    }
                    result.DateRange = $"{payload.StartDate} to {payload.EndDate}";
                    result.CustomerName = acctInfod.AccountName;
                    result.EffectiveBal = acctInfod.Effectiveavail.ToString();
                    result.AvailableBal = acctInfod.AvailableBalance.ToString();
                    result.Branch = acctInfod.Branch;
                    result.Address = acctInfod.Address;
                    result.ClosingBal = acctInfod.AvailableBalance.ToString();
                    return Ok(result);
                }

                var acctInfo = await _apiService.GetCustomerDetailByAccountNumber(payload.AccountNumber);
                if (acctInfo.ResponseCode != "00")
                {
                    return BadRequest(acctInfo.ResponseDescription);
                }
                result.DateRange = $"{payload.Period}";
                result.CustomerName = acctInfo.AccountName;
                result.EffectiveBal = acctInfo.Effectiveavail.ToString();
                result.AvailableBal = acctInfo.AvailableBalance.ToString();
                result.Branch = acctInfo.Branch;
                result.Address = acctInfo.Address;
                result.ClosingBal = acctInfo.AvailableBalance.ToString();
                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }
        [HttpPost("AddBeneficiary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> AddBeneficiary(AddBeneficiaryDto model)
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

                var parallexBankCode = _config.GetValue<string>("ParralexBankCode");
                if (CorporateProfile == null)
                {
                    return BadRequest("UnAuthorized Access");
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }
                var payload = new AddBeneficiaryDto
                {
                    AccountNumber = Encryption.DecryptStrings(model.AccountNumber),
                    AccountName = Encryption.DecryptStrings(model.AccountName),
                    BankCode = Encryption.DecryptStrings(model.BankCode),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                    BankName = Encryption.DecryptStrings(model.BankName)
                };

                if (payload.BankCode == parallexBankCode)
                {
                    //check if assessment exist
                    var intra = UnitOfWork.IntraBankBeneficiaryRepo.GetIntrabankBeneficiaryByAccountNumber(payload.AccountNumber, tblCorporateCustomer.Id);
                    if (intra != null)
                    {
                        return BadRequest("Beneficiary details already exist");
                    }

                    //call name inquiry API
                    var nameEnq = await _apiService.BankNameInquire(payload.AccountNumber, payload.BankCode);

                    if (nameEnq.ResponseCode != "00")
                    {
                        return BadRequest("Account number could not be verified");
                    }

                    //save beneficiary
                    var beneficiary = new TblIntrabankbeneficiary
                    {
                        Id = Guid.NewGuid(),
                        AccountName = nameEnq.AccountName,
                        AccountNumber = nameEnq.AccountNumber,
                        CustAuth = tblCorporateCustomer.Id,
                        DateAdded = DateTime.Now,
                        Status = 1
                    };

                    var auditTrail = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                        Ipaddress = payload.IPAddress,
                        Macaddress = payload.MACAddress,
                        ClientStaffIpaddress = payload.ClientStaffIPAddress,
                        HostName = payload.HostName,
                        NewFieldValue = $"Intra Bank Beneficiary:- AccountName: {beneficiary.AccountName}, AccountNumber: {beneficiary.AccountNumber}, Corporate Customer Id: {beneficiary.CustAuth}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = CorporateProfile.Id,
                        Username = UserName,
                        Description = $"Add Intra Bank Beneficiary. Action was carried out by a Corporate user"
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditTrail);
                    UnitOfWork.IntraBankBeneficiaryRepo.Add(beneficiary);
                }
                else
                {
                    //check if assessment exist
                    var inter = UnitOfWork.InterBankBeneficiaryRepo.GetInterbankBeneficiaryByAccountNumber(payload.AccountNumber, tblCorporateCustomer.Id);
                    if (inter != null)
                    {
                        return BadRequest("Beneficiary details already exist");
                    }
                    var nameEnq = await _apiService.BankNameInquire(payload.AccountNumber, payload.BankCode);
                    if (nameEnq.ResponseCode != "00")
                    {
                        return BadRequest("Account number could not be verified");
                    }

                    //save details
                    var beneficiary = new TblInterbankbeneficiary
                    {
                        Id = Guid.NewGuid(),
                        AccountName = nameEnq.AccountName,
                        AccountNumber = nameEnq.AccountNumber,
                        CustAuth = tblCorporateCustomer.Id,
                        DateAdded = DateTime.Now,
                        Status = 1,
                        DestinationInstitutionCode = payload.BankCode,
                        DestinationInstitutionName = payload.BankName
                    };
                    var auditTrail = new TblAuditTrail
                    {
                        Id = Guid.NewGuid(),
                        ActionCarriedOut = nameof(AuditTrailAction.Create).Replace("_", " "),
                        Ipaddress = payload.IPAddress,
                        Macaddress = payload.MACAddress,
                        ClientStaffIpaddress = payload.ClientStaffIPAddress,
                        HostName = payload.HostName,
                        NewFieldValue = $"Inter Bank Beneficiary:- AccountName: {beneficiary.AccountName},AccountNumber: {beneficiary.AccountNumber}, BankName: {beneficiary.DestinationInstitutionName},Corporate Customer Id: {beneficiary.CustAuth}",
                        PreviousFieldValue = "",
                        TransactionId = "",
                        UserId = CorporateProfile.Id,
                        Username = UserName,
                        Description = $"Add Inter Bank Beneficiary. Action was carried out by a Corporate user"
                    };
                    UnitOfWork.AuditTrialRepo.Add(auditTrail);
                    UnitOfWork.InterBankBeneficiaryRepo.Add(beneficiary);
                }
                UnitOfWork.Complete();
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }
        [HttpGet("InterbankBeneficiaries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ListResponseDTO<TblInterbankbeneficiary>> GetInterbankBeneficiaries()
        {
            //string errormsg = string.Empty;
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
                    return BadRequest("UnAuthorized Access");
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                //call statement of account API
                var dto = UnitOfWork.InterBankBeneficiaryRepo.GetInterbankBeneficiaries(tblCorporateCustomer.Id);
                if (dto == null || dto?.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(new ListResponseDTO<TblInterbankbeneficiary>(_data: dto, success: true, _message: Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }
        [HttpGet("IntrabankBeneficiaries")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ListResponseDTO<TblIntrabankbeneficiary>> GetIntrabankBeneficiaries()
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
                    return BadRequest("UnAuthorized Access");
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                //call statement of account API
                var dto = UnitOfWork.IntraBankBeneficiaryRepo.GetIntrabankBeneficiaries(tblCorporateCustomer.Id);
                if (dto == null || dto?.Count == 0)
                {
                    return StatusCode(204);
                }
                return Ok(new ListResponseDTO<TblIntrabankbeneficiary>(_data: dto, success: true, _message: Message.Success));
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }
        [HttpPost("RemoveInterbankBeneficiary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> RemoveInterbankBeneficiary(RemoveBeneficiaryDto model)
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
                    return BadRequest("UnAuthorized Access");
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                //var Id = Encryption.DecryptGuid(beneficiaryId);
                var payload = new RemoveBeneficiary
                {
                    beneficiaryId = Encryption.DecryptGuid(model.beneficiaryId),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                };
                //get beneficiary
                var bene = UnitOfWork.InterBankBeneficiaryRepo.GetInterbankBeneficiary(payload.beneficiaryId, tblCorporateCustomer.Id);
                if (!bene.Any())
                {
                    return BadRequest("Beneficiary was not found");
                }
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Remove).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    HostName = payload.HostName,
                    NewFieldValue = $"Inter Bank Beneficiary:- AccountName: {bene[0].AccountName},AccountNumber: {bene[0].AccountNumber}, BankName: {bene[0].DestinationInstitutionName},Corporate Customer Id: {bene[0].CustAuth}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Remove Inter Bank Beneficiary. Action was carried out by a Corporate user"
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.InterBankBeneficiaryRepo.RemoveRange(bene);
                UnitOfWork.Complete();
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }
        [HttpPost("RemoveIntrabankBeneficiary")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<bool> RemoveIntrabankBeneficiary(RemoveBeneficiaryDto model)
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
                    return BadRequest("UnAuthorized Access");
                }

                var tblCorporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile.CorporateCustomerId);
                if (tblCorporateCustomer == null)
                {
                    return BadRequest("Invalid corporate customer id");
                }

                var payload = new RemoveBeneficiary
                {
                    beneficiaryId = Encryption.DecryptGuid(model.beneficiaryId),
                    ClientStaffIPAddress = Encryption.DecryptStrings(model.ClientStaffIPAddress),
                    IPAddress = Encryption.DecryptStrings(model.IPAddress),
                    MACAddress = Encryption.DecryptStrings(model.MACAddress),
                    HostName = Encryption.DecryptStrings(model.HostName),
                };

                //get beneficiary
                var bene = UnitOfWork.IntraBankBeneficiaryRepo.GetIntrabankBeneficiary(payload.beneficiaryId, tblCorporateCustomer.Id);
                if (bene == null)
                {
                    return BadRequest("Beneficiary was not found");
                }
                var auditTrail = new TblAuditTrail
                {
                    Id = Guid.NewGuid(),
                    ActionCarriedOut = nameof(AuditTrailAction.Remove).Replace("_", " "),
                    Ipaddress = payload.IPAddress,
                    Macaddress = payload.MACAddress,
                    ClientStaffIpaddress = payload.ClientStaffIPAddress,
                    HostName = payload.HostName,
                    NewFieldValue = $"Inter Bank Beneficiary:- AccountName: {bene[0].AccountName},AccountNumber: {bene[0].AccountNumber},Corporate Customer Id: {bene[0].CustAuth}",
                    PreviousFieldValue = "",
                    TransactionId = "",
                    UserId = CorporateProfile.Id,
                    Username = UserName,
                    Description = $"Remove Inter Bank Beneficiary. Action was carried out by a Corporate user"
                };
                UnitOfWork.AuditTrialRepo.Add(auditTrail);
                UnitOfWork.IntraBankBeneficiaryRepo.RemoveRange(bene);
                UnitOfWork.Complete();
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("SERVER ERROR {0}, {1}, {2}", Formater.JsonType(ex.StackTrace), Formater.JsonType(ex.Source), Formater.JsonType(ex.Message));
                return ex.InnerException != null ? BadRequest(new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException.Message, responseStatus: false)) : StatusCode(500, new ErrorResponse(responsecode: ResponseCode.SERVER_ERROR, responseDescription: ex.InnerException != null ? ex.InnerException.Message : ex.Message, responseStatus: false));
            }
        }

    }
}