
using AutoMapper;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CIB.Core.Services.Authentication;
using DocumentFormat.OpenXml.InkML;

namespace CIB.CorporateAdmin.Controllers
{
	[ApiController]
	[Route("api/CorporateAdmin/v1/[controller]")]
	public class BaseAPIController : ControllerBase
	{
		protected readonly IUnitOfWork _unitOfWork;
		protected readonly IMapper _mapper;
		private string username;
		private string CustomerId;
		private string corporateCustomerId;
		private string userAgent;
		private TblCorporateProfile? corporateProfile;
		private readonly IAuthenticationService _authService;
		protected readonly IHttpContextAccessor _accessor;
		private readonly ILogger<BaseAPIController> _logger;
		/// <summary>
		/// Responsible for initializing  the factory, respository and config service objects etc.
		/// </summary>
		/// <param name="unitOfWork">A CIB UnitOfWork object</param>
		/// <param name="mapper">A CIB UnitOfWork object</param>
		/// <param name="accessor"></param>
		public BaseAPIController(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IAuthenticationService authService)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_accessor = accessor;
			_authService = authService;

		}
		protected IUnitOfWork UnitOfWork
		{
			get
			{
				return _unitOfWork;
			}
		}
		protected IAuthenticationService AuthService
		{
			get
			{
				return _authService;
			}
		}
		protected IMapper Mapper
		{
			get
			{
				return _mapper;
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
					IEnumerable<Claim>? claim = identity?.Claims;
					if (claim != null)
					{
						var usernameClaim = claim.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
						if (usernameClaim == null)
						{
							return string.Empty;
						}
						username = usernameClaim.Value;
						CustomerId = claim?.FirstOrDefault(x => x.Type == "CustomerID").Value;
					}
				}
				return username;
			}
		}
		protected string UserAgent
		{
			get
			{
				userAgent = HttpContext?.Request?.Headers["User-Agent"].FirstOrDefault() ?? "";
				return userAgent;
			}
		}
		protected TblCorporateProfile? CorporateProfile
		{
			get
			{
				if ((corporateProfile == null && CustomerId != "") || CustomerId == null)
				{

					if (UserName != null)
					{
						if (string.IsNullOrEmpty(CorporateCustomerId))
						{
							corporateProfile = null;
						}
						else
						{
							corporateProfile = UnitOfWork.CorporateProfileRepo.GetProfileByUserNameAndCustomerId(UserName.Trim().ToLower(), Guid.Parse(CorporateCustomerId));
							if (corporateProfile != null)
							{
								// string token = HttpContext.GetToken();
								var items = HttpContext.Items;
								// Get token  
								if (items == null || items?.Count == 0)
								{
									corporateProfile = null;
								}
								// Gets name from claims. Generally it's an email address.

								string? token = items?.Where(x => x.Key.ToString() == "token")?.FirstOrDefault().Value?.ToString();
								if (!string.IsNullOrEmpty(token))
								{
									var result = _authService.GetPrincipalFromExpiredToken(token);
									if (result is null)
									{
										return null;
									}

									bool isTokenStillValid = _unitOfWork.TokenBlackCorporateRepo.IsTokenStillValid(corporateProfile.Id, token);
									corporateProfile = isTokenStillValid == true ? corporateProfile : null;
								}
								else
								{
									corporateProfile = null;
								}
							}

						}
					}
				}
				return corporateProfile;
			}
		}
		protected string CorporateCustomerId
		{
			get
			{
				if (string.IsNullOrEmpty(corporateCustomerId))
				{
					if (HttpContext.User == null)
					{
						return string.Empty;
					}
					var identity = HttpContext.User.Identity as ClaimsIdentity;
					// Gets list of claims.
					IEnumerable<Claim>? claim = identity?.Claims;
					if (claim != null)
					{
						//var usernameClaim = claim.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
						var CustomerId = claim?.FirstOrDefault(x => x.Type == "CorporateCustomerId")?.Value;
						if (CustomerId == null)
						{
							return string.Empty;
						}
						corporateCustomerId = CustomerId;
					}
				}
				return corporateCustomerId;
			}
		}
		protected bool IsAuthenticated => CorporateProfile != null ? true : false;
		protected string? UserRoleId => CorporateProfile != null ? CorporateProfile?.CorporateRole?.ToString() : null;
		protected bool IsUserActive(out string errormsg)
		{
			StatusResponse result;
			if (CorporateProfile != null)
			{
				result = new AccountStatus().CheckCorporateUserAccountStatus(CorporateProfile);
				if (!result.Status)
				{
					errormsg = result.Message;
					return result.Status;
				}
				var corporateCustomer = UnitOfWork.CorporateCustomerRepo.GetByIdAsync((Guid)CorporateProfile?.CorporateCustomerId);
				var customerResult = new AccountStatus().CheckCorporateCustomerAccountStatus(corporateCustomer);
				if (!customerResult.Status)
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