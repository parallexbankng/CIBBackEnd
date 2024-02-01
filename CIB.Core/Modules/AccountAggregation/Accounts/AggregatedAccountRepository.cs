using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;

namespace CIB.Core.Modules.AccountAggregation.Accounts;

public class AggregatedAccountRepository : Repository<TblAggregatedAccount>, IAggregatedAccountRepository
{
	public AggregatedAccountRepository(ParallexCIBContext context) : base(context)
	{
	}
	public ParallexCIBContext context { get { return _context as ParallexCIBContext; } }

	public List<TblAggregatedAccount> GetCorporateAggregationAccountByAggregationId(Guid corporateCustomerId)
	{
		return _context.TblAggregatedAccounts.Where(ctx => ctx.CorporateCustomerId == corporateCustomerId).ToList();
	}
	public List<TblAggregatedAccount> GetCorporateAggregationAccountByAggregateId(Guid aggregatedId)
	{
		return _context.TblAggregatedAccounts.Where(ctx => ctx.AccountAggregationId == aggregatedId).ToList();
	}

	public TblAggregatedAccount GetCorporateAggregationAccountByAccountNumber(string accountNumber)
	{
		return _context.TblAggregatedAccounts.Where(ctx => ctx.AccountNumber == accountNumber).FirstOrDefault();
	}
	public TblAggregatedAccount GetCorporateAggregationAccountByAccountNumberAndCorporateCustomer(string accountNumber, Guid corporateCustomerId)
	{
		return _context.TblAggregatedAccounts.FirstOrDefault(ctx => ctx.AccountNumber.Trim() == accountNumber.Trim() && ctx.CorporateCustomerId == corporateCustomerId);
	}
}
