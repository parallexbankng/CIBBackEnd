using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.AccountAggregationTemp.Aggregations;
public class TempCorporateAggregationRepository : Repository<TblTempCorporateAccountAggregation>, ITempCorporateAggregationRepository
{
	public TempCorporateAggregationRepository(ParallexCIBContext context) : base(context)
	{
	}
	public ParallexCIBContext context { get { return _context as ParallexCIBContext; } }
	public List<TblTempCorporateAccountAggregation> GetCorporateCustomerAggregationByAggregationCustomerId(string aggregationCustomerId)
	{
		return _context.TblTempCorporateAccountAggregations.Where(ctx => ctx.CustomerId == aggregationCustomerId).ToList();

	}
	public TblTempCorporateAccountAggregation GetCorporateCustomerAggregations(Guid aggregationId)
	{
		return _context.TblTempCorporateAccountAggregations.Where(ctx => ctx.Id == aggregationId).FirstOrDefault();
	}
	public List<TblTempCorporateAccountAggregation> GetPendingCorporateCustomerAggregations(Guid? corporateCustomerId)
	{
		return _context.TblTempCorporateAccountAggregations.Where(ctx => ctx.CorporateCustomerId == corporateCustomerId && ctx.IsTreated == 0).ToList();
	}
	public List<AggregationResponses> GetPendingCorporateCustomerAggregationWithAccounts(Guid? corporateCustomerId)
	{
		var result = new List<AggregationResponses>();
		var acountResult = _context.TblTempCorporateAccountAggregations.Where(ctx => ctx.CorporateCustomerId == corporateCustomerId).ToList();

		return result;
	}

	public TempAggregationResponses GetCorporateAggregationAccountByAggregationId(Guid aggregationCustomerId)
	{
		//var result = new AggregationResponses();
		var account = _context.TblTempCorporateAccountAggregations.FirstOrDefault(ctx => ctx.Id == aggregationCustomerId);
		var result = new TempAggregationResponses
		{
			Id = account.Id,
			Sn = account.Sn,
			CorporateCustomerId = account.CorporateCustomerId,
			AccountAggregationId = account.AccountAggregationId,
			InitiatorId = account.InitiatorId,
			DefaultAccountNumber = account.DefaultAccountNumber,
			DefaultAccountName = account.DefaultAccountName,
			CustomerId = account.CustomerId,
			InitiatorUserName = account.InitiatorUserName,
			Status = account.Status,
			Action = account.Action,
			DateInitiated = account.DateInitiated,
		};
		result.AccountNumbers = _context.TblTempAggregatedAccounts.Where(ctx => ctx.AccountAggregationId == account.Id).ToList();
		return result;
	}

	public TblTempCorporateAccountAggregation GetCorporateCustomerAggregationByID(string aggregateCustomerId, Guid? corporateCustomerId)
	{
		return _context.TblTempCorporateAccountAggregations.Where(ctx => ctx.CustomerId == aggregateCustomerId && ctx.CorporateCustomerId == corporateCustomerId).FirstOrDefault();
	}
	public CorporateUserStatus CheckDuplicate(TblTempCorporateAccountAggregation profile, bool IsUpdate = false)
	{
		var customerId = _context.TblTempCorporateAccountAggregations.FirstOrDefault(x => x.CustomerId.Trim().Equals(profile.CustomerId.Trim()) && x.IsTreated == 0);
		var defaultAccountNumber = _context.TblTempCorporateAccountAggregations.FirstOrDefault(x => x.CustomerId.Trim().Equals(profile.CustomerId.Trim()) && x.IsTreated == 0);
		if (customerId != null)
		{
			if (IsUpdate)
			{
				if (profile.Id != customerId.Id)
				{
					return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists and is pending approval", IsDuplicate = "01" };
				}
			}
			else
			{
				return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists and is pending approval", IsDuplicate = "01" };
			}
		}
		if (defaultAccountNumber != null)
		{
			if (IsUpdate)
			{
				if (profile.Id != defaultAccountNumber.Id)
				{
					return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists and is pending approval", IsDuplicate = "01" };
				}
			}
			else
			{
				return new CorporateUserStatus { Message = "Corporate Account Aggregation Already Exists and is pending approval", IsDuplicate = "01" };
			}
		}
		return new CorporateUserStatus { Message = "", IsDuplicate = "02" };
	}

	public void UpdateAccountAggregation(TblTempCorporateAccountAggregation request)
	{
		_context.Update(request).Property(x => x.Sn).IsModified = false;
	}

	public TblTempCorporateAccountAggregation GetCorporateCustomerAggregation(Guid aggregationId, Guid? corporateCustomerId)
	{
		return _context.TblTempCorporateAccountAggregations.FirstOrDefault(ctx => ctx.Id == aggregationId && ctx.CorporateCustomerId == corporateCustomerId);
	}

	public TblTempCorporateAccountAggregation GetCorporateCustomerAggregationByID(Guid Id, Guid? corporateCustomerId)
	{
		return _context.TblTempCorporateAccountAggregations.Where(ctx => ctx.Id == Id && ctx.CorporateCustomerId == corporateCustomerId).FirstOrDefault();
	}
}