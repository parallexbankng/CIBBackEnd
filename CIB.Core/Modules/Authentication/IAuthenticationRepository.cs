using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Interface;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.Authentication
{
	public interface IBankAuthenticationRepository : IRepository<TblBankProfile>
	{
		LoginResponsedata BankUserLoginWithActiveDirectory(BankUserLoginParam login);
		TblBankProfile BankUserLogin(BankUserLoginParam login);

	}

	public interface ICustomerAuthenticationRepository : IRepository<TblCorporateProfile>
	{
		TblCorporateProfile VerifyCorporateProfileUserName(CustomerLoginParam login, Guid CorporateCustomerId);
		TblCorporateCustomer VerifyCorporateProfileByCustomerId(string id);
		TblCorporateCustomer VerifyCorporateCustomerById(Guid corporateCustomerId);
	}
}