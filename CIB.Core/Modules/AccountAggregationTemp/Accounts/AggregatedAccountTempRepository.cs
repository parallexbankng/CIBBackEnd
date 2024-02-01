using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.AccountAggregation.Accounts.Dto;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CIB.Core.Modules.AccountAggregationTemp.Accounts;

public class TempAggregatedAccountRepository : Repository<TblTempAggregatedAccount>, ITempAggregatedAccountRepository
{
	public TempAggregatedAccountRepository(ParallexCIBContext context) : base(context)
	{
	}
	public ParallexCIBContext context { get { return _context as ParallexCIBContext; } }


	public List<TblTempAggregatedAccount> GetAllCorporateAggregationAccountByAggregationId(Guid? aggregationId)
	{
		return _context.TblTempAggregatedAccounts.Where(ctx => ctx.AccountAggregationId == aggregationId).ToList();
	}
	public TblTempAggregatedAccount GetCorporateCustomerAggregationTempByAggregationCustomerID()
	{
		throw new System.NotImplementedException();
	}

	public TblTempAggregatedAccount GetCorporateCustomerAggregationTempByAggregationCustomerID(Guid aggregationId)
	{
		throw new NotImplementedException();
	}

	public void UpdateAggregatedAccount(TblTempAggregatedAccount request)
	{

		_context.Update(request).Property(x => x.Sn).IsModified = false;

	}

	public void UpdateAggregatedAccountList(List<TblTempAggregatedAccount> request)
	{
		foreach (var item in request)
		{
			_context.Update(item).Property(x => x.Sn).IsModified = false;
		}
	}
}
