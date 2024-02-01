using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateCustomer.Dto;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.CorporateCustomer
{
  public class CorporateCustomerRepository : Repository<TblCorporateCustomer>, ICorporateCustomerRepository
  {
    public CorporateCustomerRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public IEnumerable<TblCorporateCustomer> GetAllCorporateCustomers()
    {
      return _context.TblCorporateCustomers.OrderByDescending(ctx => ctx.Sn).ToList();

    }

    public TblCorporateCustomer GetCorporateCustomerByCompanyName(string companyName)
    {
      throw new NotImplementedException();
    }


    public TblCorporateCustomer GetCorporateCustomerByCustomerID(string id)
    {
      return _context.TblCorporateCustomers.SingleOrDefault(a => a.CustomerId.Trim() == id.Trim());
      return _context.TblCorporateCustomers.SingleOrDefault(a => a.CustomerId.Trim() == id.Trim());
    }

    public void UpdateCorporateCustomer(TblCorporateCustomer profile)
    {
      _context.Update(profile).Property(x => x.Sn).IsModified = false;
    }

    public CorporateUserStatus CheckDuplicate(TblCorporateCustomer profile, bool IsUpdate = false)
    {
      var duplicateEmail = _context.TblCorporateCustomers.FirstOrDefault(x => x.CustomerId.Trim().Equals(profile.CustomerId.Trim()) && x.Status == 1);
      if (duplicateEmail != null)
      {
        if (IsUpdate)
        {
          if (profile.Id != duplicateEmail.Id)
          {
            return new CorporateUserStatus { Message = "Corporate Customer Already Exit", IsDuplicate = "01" };
          }
        }
        else
        {
          return new CorporateUserStatus { Message = "Corporate Customer Already Exit", IsDuplicate = "01" };
        }
      }
      return new CorporateUserStatus { Message = "", IsDuplicate = "02" };
    }

    public TblCorporateCustomer CheckDuplicateCorporateShortName(string corporateShortName)
    {
      return _context.TblCorporateCustomers.FirstOrDefault(x => x.CorporateShortName.Trim().Equals(corporateShortName.Trim()));
    }

    public TblCorporateCustomer GetCustomerByCustomerId(string customerId)
    {
      return _context.TblCorporateCustomers.SingleOrDefault(a => a.CustomerId.Trim().ToLower() == customerId.Trim().ToLower());
    }

    public IEnumerable<ChangeSignatoryDto> Search(string CompanyName, string Signatory)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<ChangeSignatoryDto> GetCorporateCustomerWhoChangeSigntory()
    {
      throw new NotImplementedException();
    }
  }
}