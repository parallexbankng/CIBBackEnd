using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CIB.BankAdmin.Controllers
{
    [ApiController]
    [Route("api/BankAdmin/v1/[controller]")]
    public class BaseAPIController : ControllerBase
    {
        protected TblBankProfile bankProfile;
        protected readonly IHttpContextAccessor _accessor;
        protected readonly IMapper _mapper;
        protected readonly IUnitOfWork _unitOfWork;
        private string username;
        private string CustomerId;

        /// <summary>
        /// Responsible for initializing  the factory, respository and config service objects etc.
        /// </summary>
        /// <param name="mapper">A CIB UnitOfWork object</param>
        /// <param name="unitOfWork">A CIB UnitOfWork object</param>
        /// <param name="accessor"> Accessor </param>
        public BaseAPIController(IMapper mapper,IUnitOfWork unitOfWork, IHttpContextAccessor accessor)
        {
            this._mapper = mapper;
            this._accessor = accessor;
            this._unitOfWork = unitOfWork;
        }

        protected IUnitOfWork UnitOfWork
        {
            get { return _unitOfWork; }
        }

        protected IMapper Mapper
        {
            get { return _mapper; }
        }

        protected TblBankProfile BankProfile
        {
            get
            {
                if((bankProfile == null && CustomerId?.Length == 0) || CustomerId == null)
                {
                    if(UserName != null){
                        bankProfile = UnitOfWork.BankProfileRepo.GetProfileByUserName(UserName.Trim());
                        if(bankProfile != null)
                        {
                           // string token = HttpContext.GetToken();
                            var items = HttpContext.Items;
                            // Get token  
                            if (items == null || items?.Count == 0)
                            {
                                bankProfile = null;
                            }
                            // Gets name from claims. Generally it's an email address.
                            var token = items.Where(x => x.Key.ToString() == "token")?.FirstOrDefault().Value?.ToString();
                            if (!string.IsNullOrEmpty(token))
                            {
                                bool isTokenStillValid = _unitOfWork.TokenBlackRepo.IsTokenStillValid(bankProfile.Id, token);
                                bankProfile = isTokenStillValid ? bankProfile : null;
                            }
                            else
                            {
                                bankProfile = null;
                            }
                        }
                    }
                }
                return bankProfile;
            }
        }


      

        protected string UserName
        {
            get
            {
                if (string.IsNullOrEmpty(username))
                {
                    if (HttpContext.User == null)
                    {
                        return string.Empty;
                    }
                    var identity = HttpContext.User.Identity as ClaimsIdentity;
                    // Gets list of claims.
                    IEnumerable<Claim> claim = identity.Claims;
                    if (claim != null)
                    {
                        var usernameClaim = claim.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
                        if (usernameClaim == null)
                        {
                            return string.Empty;
                        }
                        username = usernameClaim.Value;
                        CustomerId = claim.FirstOrDefault(x => x.Type == "CustomerID").Value;
                    }
                }
                return username;
            }
        }

    
        protected bool IsAuthenticated => BankProfile != null;
        protected string UserRoleId => BankProfile != null ? bankProfile?.UserRoles?.ToString() : null;

        protected bool IsUserActive(out string errormsg)
        {
            StatusResponse result;
            if(BankProfile != null){
                result = new AccountStatus().CheckAdminAccountStatus(BankProfile);
                if (!result.Status)
                {
                errormsg = result.Message;
                return result.Status;
                }
                errormsg = result.Message;
                return result.Status;
            }
            errormsg = "error";
            return false;
        }
    }
}