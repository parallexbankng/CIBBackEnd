using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TemCorporateCustomer
{
    public class TemCorporateCustomerRespository: Repository<TblTempCorporateCustomer>, ITemCorporateCustomerRespository
    {
        public TemCorporateCustomerRespository(ParallexCIBContext context) : base(context)
        {

        }
        public ParallexCIBContext context
        {
          get { return _context as ParallexCIBContext; }
        }

        public List<TblTempCorporateCustomer> GetCorporateCustomerPendingApproval(int isTreated)
        {
          return _context.TblTempCorporateCustomers.Where(ctx => ctx.IsTreated == isTreated).ToList();
        }

        public void UpdateTemCorporateCustomer(TblTempCorporateCustomer update)
        {
          _context.Update(update).Property(x=>x.Sn).IsModified = false;
        }

        public List<TblTempCorporateCustomer> CheckDuplicateRequest(TblCorporateCustomer profile, string action)
        {
          return _context.TblTempCorporateCustomers.Where(ctx => ctx.IsTreated == (int)ProfileStatus.Pending && ctx.Action == action && ctx.CorporateCustomerId == profile.Id).ToList();
        }

        public CorporateUserStatus CheckDuplicate(TblTempCorporateCustomer profile, bool IsUpdate)
        {
          var duplicateEmail = _context.TblTempCorporateCustomers.FirstOrDefault(x => x.CustomerId.Trim().Equals(profile.CustomerId.Trim()));
          if(duplicateEmail != null)
          {
            if(IsUpdate)
            {
              if(profile.Id != duplicateEmail.Id)
              {
                return new CorporateUserStatus { Message = "Corporate Customer Already Exit or is Pending Approval", IsDuplicate = "01" };
              }
            }
            else
            {
              return new CorporateUserStatus { Message = "Corporate Customer Already Exit or is Pending Approval", IsDuplicate = "01" };
            }
          }
          return new CorporateUserStatus { Message = "", IsDuplicate = "02" };
        }

        public TblTempCorporateCustomer GetCorporateCustomerByCustomerID(string id)
        {
          return _context.TblTempCorporateCustomers.FirstOrDefault(a => a.CustomerId == id);
        }
  }
}