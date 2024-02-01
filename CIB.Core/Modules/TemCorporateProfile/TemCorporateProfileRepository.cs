using System.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.TemCorporateProfile
{
  public class TemCorporateProfileRepository : Repository<TblTempCorporateProfile>, ITemCorporateProfileRepository
  {
    public TemCorporateProfileRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public List<TblTempCorporateProfile> GetCorporateProfilePendingApproval(int isTreated, Guid CorporateCustormerId)
    {
      return _context.TblTempCorporateProfiles.Where(ctx => ctx.IsTreated == isTreated && ctx.CorporateCustomerId != null && ctx.CorporateCustomerId == CorporateCustormerId).ToList();
    }

    public List<TblTempCorporateProfile> GetCorporateProfilePendingApproval(int isTreated)
    {
      return _context.TblTempCorporateProfiles.Where(ctx => ctx.IsTreated == isTreated).ToList();
    }

    public void UpdateTempCorporateProfile(TblTempCorporateProfile update)
    {
      _context.Update(update).Property(x => x.Sn).IsModified = false;
    }

    public CorporateUserStatus CheckDuplicate(TblTempCorporateProfile profile, Guid CorporateCustomerId, bool isUpdate)
    {
      var duplicatUsername = _context.TblTempCorporateProfiles.FirstOrDefault(x => x.Username != null && x.Username.Trim().ToLower().Equals(profile.Username.Trim().ToLower()) && x.CorporateCustomerId == CorporateCustomerId && x.IsTreated == 0);
      var duplicatePhone = _context.TblTempCorporateProfiles.FirstOrDefault(x => x.Phone1 != null && x.Phone1.Trim().Equals(profile.Phone1.Trim()) && x.CorporateCustomerId == CorporateCustomerId && x.IsTreated == 0);
      var duplicateEmail = _context.TblTempCorporateProfiles.FirstOrDefault(x => x.Email != null && x.Email.Trim().ToLower().Equals(profile.Email.Trim().ToLower()) && x.CorporateCustomerId == CorporateCustomerId && x.IsTreated == 0);

      if (duplicatUsername != null)
      {
        if (isUpdate)
        {
          if (profile.CorporateProfileId != duplicatUsername.CorporateProfileId)
          {
            return new CorporateUserStatus { Message = "User Name Already Exit", IsDuplicate = "01" };
          }
        }
        else
        {
          return new CorporateUserStatus { Message = "User Name Already Exit", IsDuplicate = "01" };
        }
      }
      if (duplicateEmail != null)
      {
        if (isUpdate)
        {
          if (profile.CorporateProfileId != duplicateEmail.CorporateProfileId)
          {
            return new CorporateUserStatus { Message = "Email Address Already Exit", IsDuplicate = "01" };
          }
        }
        else
        {
          return new CorporateUserStatus { Message = "Email Address Already Exit", IsDuplicate = "01" };
        }

      }
      if (duplicatePhone != null)
      {
        if (isUpdate)
        {
          if (profile.CorporateProfileId != duplicatePhone.CorporateProfileId)
          {
            return new CorporateUserStatus { Message = "Phone Number Already Exit", IsDuplicate = "01" };
          }
        }
        else
        {
          return new CorporateUserStatus { Message = "Phone Number Already Exit", IsDuplicate = "01" };
        }

      }
      return new CorporateUserStatus { Message = "", IsDuplicate = "02" };
    }

    public TblTempCorporateProfile CheckDuplicateUserName(string userName)
    {
      return _context.TblTempCorporateProfiles.FirstOrDefault(x => x.Username.Trim().Equals(userName.Trim()));
    }
  }
}