using System;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.Authentication.Dto;

namespace CIB.Core.Modules.Authentication
{
  public class BankAuthenticationRepository : Repository<TblBankProfile>, IBankAuthenticationRepository
  {

    public BankAuthenticationRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
        get { return _context as ParallexCIBContext; }
    }

    public TblBankProfile BankUserLogin(BankUserLoginParam model)
    {
      return _context.TblBankProfiles.Where(a => a.Username == model.Username).FirstOrDefault();
    }

    public LoginResponsedata BankUserLoginWithActiveDirectory(BankUserLoginParam login)
    {
      throw new NotImplementedException();
    }

  }

 public class CustomerAuthenticationRepository : Repository<TblCorporateProfile>, ICustomerAuthenticationRepository
  {
    public CustomerAuthenticationRepository(ParallexCIBContext context) : base(context)
    {
    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public TblCorporateCustomer VerifyCorporateProfileByCustomerId(string id)
    {
      return  _context.TblCorporateCustomers.Where(a => a.CustomerId == id).FirstOrDefault();
    }

    public TblCorporateProfile VerifyCorporateProfileUserName(CustomerLoginParam login, Guid CorporateCustomerId)
    {
      return  _context.TblCorporateProfiles.Where(a => a.Username.ToLower().Trim() == login.Username.ToLower().Trim() && a.CorporateCustomerId == CorporateCustomerId).FirstOrDefault();
    }
    
  }
}