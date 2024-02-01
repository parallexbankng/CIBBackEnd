using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AutoMapper;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Services.Authentication;
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
		private readonly IAuthenticationService _authService;
		private string username;
		private string userId;
		private string CustomerId;

		/// <summary>
		/// Responsible for initializing  the factory, respository and config service objects etc.
		/// </summary>
		/// <param name="mapper">A CIB UnitOfWork object</param>
		/// <param name="unitOfWork">A CIB UnitOfWork object</param>
		/// <param name="accessor"> Accessor </param>
		public BaseAPIController(IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor accessor, IAuthenticationService authService)
		{
			this._mapper = mapper;
			this._accessor = accessor;
			this._unitOfWork = unitOfWork;
			this._authService = authService;
		}
		protected IUnitOfWork UnitOfWork
		{
			get { return _unitOfWork; }
		}
		protected IMapper Mapper
		{
			get { return _mapper; }
		}
		protected IAuthenticationService AuthService
		{
			get
			{
				return _authService;
			}
		}
		protected TblBankProfile BankProfile
		{
			get
			{
				if ((bankProfile == null && CustomerId?.Length == 0) || CustomerId == null)
				{
					if (UserId != "")
					{
						bankProfile = UnitOfWork.BankProfileRepo.GetByIdAsync(Guid.Parse(userId));
						if (bankProfile != null)
						{
							// string token = HttpContext.GetToken();
							var items = HttpContext?.Items;
							// Get token  
							if (items == null || items?.Count == 0)
							{
								bankProfile = null;
							}
							// Gets name from claims. Generally it's an email address.
							var token = items?.Where(x => x.Key.ToString() == "token")?.FirstOrDefault().Value?.ToString();
							if (!string.IsNullOrEmpty(token))
							{
								var result = _authService.GetPrincipalFromExpiredToken(token);
								if (result is null)
								{
									return null;
								}
								bool isTokenStillValid = _unitOfWork.TokenBlackRepo.IsTokenStillValid(bankProfile.Id, token);
								bankProfile = isTokenStillValid == true ? bankProfile : null;
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
				return BankProfile != null ? bankProfile?.Username : null;
			}
		}
		protected string UserId
		{
			get
			{
				if (string.IsNullOrEmpty(username))
				{
					if (HttpContext?.User == null)
					{
						return string.Empty;
					}
					var identity = HttpContext?.User?.Identity as ClaimsIdentity;
					IEnumerable<Claim> claim = identity.Claims;
					if (identity?.Claims != null)
					{
						var usernameClaim = claim?.Where(x => x.Type == ClaimTypes.NameIdentifier)?.FirstOrDefault();
						if (usernameClaim == null)
						{
							return string.Empty;
						}
						userId = usernameClaim.Value;
					}
				}
				return userId;
			}
		}
		protected bool IsAuthenticated
		{
			get
			{
				return BankProfile != null ? true : false;
			}
		}
		protected string UserRoleId
		{
			get
			{
				return BankProfile != null ? bankProfile?.UserRoles?.ToString() : null;
			}
		}
		protected bool IsUserActive(out string errormsg)
		{
			StatusResponse result;
			if (BankProfile != null)
			{
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