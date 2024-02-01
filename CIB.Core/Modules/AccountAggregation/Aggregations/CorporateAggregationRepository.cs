using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.AccountAggregation.Aggregations;

public class CorporateAggregationRepository : Repository<TblCorporateAccountAggregation>, ICorporateAggregationRepository
{
	public CorporateAggregationRepository(ParallexCIBContext context) : base(context)
	{
	}
	public ParallexCIBContext context { get { return _context as ParallexCIBContext; } }

	public CorporateUserStatus CheckDuplicate(TblCorporateAccountAggregation profile, bool IsUpdate = false)
	{
		var customerIds = _context.TblCorporateAccountAggregations.Where(x => x.CustomerId.Trim().Equals(profile.CustomerId.Trim())).ToList();
		var defaultAccountNumbers = _context.TblCorporateAccountAggregations.Where(x => x.DefaultAccountNumber.Trim().Equals(profile.DefaultAccountNumber.Trim())).ToList();

		var resultmap = new List<CorporateUserStatus>();

		if (customerIds.Any())
		{
			if (IsUpdate)
			{
				foreach (var account in customerIds)
				{
					if (profile.Id != account.Id)
					{
						resultmap.Add(new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" });
					}
				}
			}
			else
			{
				foreach (var account in customerIds)
				{
					//if (profile.Id != account.Id)
					//{
					//	return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" };
					//}
					if (account.Status != (int)ProfileStatus.Deactivated)
					{
						//return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists", IsDuplicate = "01" };
						resultmap.Add(new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" });
					}
				}
			}
		}
		if (defaultAccountNumbers.Any())
		{
			if (IsUpdate)
			{
				//if (profile.Id != defaultAccountNumber.Id)
				//{
				//	return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" };
				//}
				foreach (var account in defaultAccountNumbers)
				{
					if (profile.Id != account.Id)
					{
						resultmap.Add(new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" });
						//return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" };
					}
				}
			}
			else
			{
				foreach (var account in defaultAccountNumbers)
				{
					//if (profile.Id != account.Id)
					//{
					//	return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" };
					//}

					if (account.Status != (int)ProfileStatus.Deactivated)
					{
						resultmap.Add(new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists ", IsDuplicate = "01" });
						//return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists", IsDuplicate = "01" };
					}
					// else
					// {
					// 	return new CorporateUserStatus { Message = "", IsDuplicate = "02" };
					// }
				}
			}
		}
		if (resultmap.Any())
		{
			return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists", IsDuplicate = "01" };
		}
		else
		{
			return new CorporateUserStatus { Message = "", IsDuplicate = "02" };
		}


	}
	public TblCorporateAccountAggregation GetCorporateAggregationByAggregationCustomerId(Guid? aggregationCustomerId)
	{
		return _context.TblCorporateAccountAggregations.Where(ctx => ctx.Id == aggregationCustomerId).FirstOrDefault();
	}
	public AggregationResponses GetAggregationByAggregationCustomerId(Guid? aggregationCustomerId)
	{
		var account = _context.TblCorporateAccountAggregations.Where(ctx => ctx.CorporateCustomerId == aggregationCustomerId && ctx.Status == (int)ProfileStatus.Active).FirstOrDefault();
		var result = new AggregationResponses
		{
			Id = account.Id,
			Sn = account.Sn,
			CorporateCustomerId = account.CorporateCustomerId,
			DefaultAccountNumber = account.DefaultAccountNumber,
			DefaultAccountName = account.DefaultAccountName,
			CustomerId = account.CustomerId,
			Status = account.Status,
			DateCreated = account.DateCreated,
		};
		result.AccountNumbers = _context.TblAggregatedAccounts.Where(ctx => ctx.AccountAggregationId == account.Id).ToList();
		return result;
	}

	public List<TblCorporateAccountAggregation> GetCorporateCustomerAggregationByAggregationCustomerId(string aggregationCustomerId)
	{
		return _context.TblCorporateAccountAggregations.Where(ctx => ctx.CustomerId == aggregationCustomerId).ToList();
	}

	public TblCorporateAccountAggregation GetCorporateCustomerAggregationByID(Guid id, Guid? corporateCustomerId)
	{
		return _context.TblCorporateAccountAggregations.Where(ctx => ctx.Id == id && ctx.CorporateCustomerId == corporateCustomerId).FirstOrDefault();
	}

	public List<TblCorporateAccountAggregation> GetCorporateCustomerAggregations(Guid? corporateCustomerId)
	{
		return _context.TblCorporateAccountAggregations.Where(ctx => ctx.CorporateCustomerId == corporateCustomerId && ctx.Status == (int)ProfileStatus.Active).ToList();
	}

	public List<TblCorporateAccountAggregation> AdminGetCorporateCustomerAggregations(Guid? corporateCustomerId)
	{
		return _context.TblCorporateAccountAggregations.Where(ctx => ctx.CorporateCustomerId == corporateCustomerId).ToList();
	}

	public void UpdateAccountAggregation(TblCorporateAccountAggregation request)
	{
		_context.Update(request).Property(x => x.Sn).IsModified = false;
	}

	public ItemStatus CheckDuplicateAggregate(TblCorporateAccountAggregation profile, bool reactivate = false)
	{
		var accountAggregationList = _context.TblCorporateAccountAggregations.Where(ctx => ctx.CorporateCustomerId == profile.CorporateCustomerId && profile.DefaultAccountNumber == ctx.DefaultAccountNumber && ctx.Id != profile.Id).ToList();
		if (accountAggregationList.Any())
		{
			foreach (var account in accountAggregationList)
			{
				if (account.Status != (int)ProfileStatus.Deactivated)
				{
					return new ItemStatus { Message = "Corporate Account Aggregation Already Exists", IsDuplicate = false };
				}
			}
		}
		return new ItemStatus { Message = "Ok", IsDuplicate = true };
	}
}