using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Enums;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Modules.TemBankAdminProfile
{
  public class TemBankAdminProfileRepository : Repository<TblTempBankProfile>, ITemBankAdminProfileRepository
  {
    public TemBankAdminProfileRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public void UpdateTemBankAdminProfile(TblTempBankProfile update)
    {
        _context.Update(update).Property(x=>x.Sn).IsModified = false;
    }

    public TblTempBankProfile GetBankProfilePendingApproval(TblBankProfile profile, int isTreated)
    {
      return _context.TblTempBankProfiles.FirstOrDefault(ctx => ctx.IsTreated == isTreated);
    }

    public List<TblTempBankProfile> GetBankProfilePendingApprovals(int isTreated)
    {
        return _context.TblTempBankProfiles.Where(ctx => ctx.IsTreated == isTreated).ToList();
    }

    public List<TblTempBankProfile> CheckDuplicateRequest(TblBankProfile profile, string Action)
    {
      return _context.TblTempBankProfiles.Where(ctx => ctx.IsTreated == (int)ProfileStatus.Pending && ctx.Action == Action && ctx.BankProfileId == profile.Id).ToList();
    }

    public AdminUserStatus CheckDuplicate(TblBankProfile update, Guid? profileId)
    {
      var duplicatePhone = _context.TblTempBankProfiles.Where(x => x.Phone == update.Phone).Any();
      var duplicateEmail = _context.TblTempBankProfiles.Where(x => x.Email.ToLower().Trim() == update.Email.ToLower().Trim()).Any();
      var duplicateUserName = _context.TblTempBankProfiles.Where(x => x.Username.Equals(update.Username)).Any();
      if(duplicateUserName)
        {
          if(profileId.HasValue)
          {
            if(update.Id != profileId.Value)
            {
              return new AdminUserStatus { Message = "User With this User Name Already Exit", IsDuplicate = true };
            }
          }
          else
          {
            return new AdminUserStatus { Message = "User With this User Name Already Exit", IsDuplicate = true };
          }
        }
      if(duplicateEmail)
      {
        if(profileId.HasValue)
        {
          if(update.Id != profileId.Value)
          {
            return new AdminUserStatus { Message = "User With this Email Address Already Exit", IsDuplicate = true };
          }
        }
        else
        {
          return new AdminUserStatus { Message = " User With this Email Address Already Exit", IsDuplicate = true };
        }
      }
      if(duplicatePhone)
      {
        if(profileId.HasValue)
        {
          if(update.Id != profileId.Value)
          {
            return new AdminUserStatus { Message = "User With this Phone Number Already Exit", IsDuplicate = true };
          }
        }
        else
        {
          return new AdminUserStatus { Message = "User With this Phone Number Already Exit", IsDuplicate = true };
        }
      }
      return new AdminUserStatus { Message = "ok", IsDuplicate = false };
    }
  
    public AdminUserStatus CheckDuplicate(TblTempBankProfile profile, bool isUpdate)
    {
      var duplicatUsername = _context.TblTempBankProfiles.FirstOrDefault(x => x.Username != null && x.Username.Trim().ToLower().Equals(profile.Username.Trim().ToLower()) && x.IsTreated == 0);
      var duplicatePhone = _context.TblTempBankProfiles.FirstOrDefault(x => x.Phone != null && x.Phone.Trim().Equals(profile.Phone.Trim()) && x.IsTreated == 0);
      var duplicateEmail = _context.TblTempBankProfiles.FirstOrDefault(x => x.Email != null && x.Email.Trim().ToLower().Equals(profile.Email.Trim().ToLower()) && x.IsTreated == 0);

      if(duplicatUsername != null)
      {
        if(isUpdate)
        {
          if(profile.BankProfileId != duplicatUsername.BankProfileId)
          {
            return new AdminUserStatus { Message = "User Name Already Exit", IsDuplicate = true };
          }
        }
        else 
        {
          return new AdminUserStatus { Message = "User Name Already Exit", IsDuplicate = true };
        }
      }
      if(duplicateEmail != null)
      {
        if(isUpdate)
        {
          if(profile.BankProfileId != duplicatUsername.BankProfileId)
          {
            return new AdminUserStatus { Message = "Email Address Already Exit", IsDuplicate = true};
          }
        }
        else
        {
          return new AdminUserStatus { Message = "Email Address Already Exit", IsDuplicate = true};
        }
        
      }
      if(duplicatePhone != null)
      {
        if(isUpdate)
        {
          if(profile.BankProfileId != duplicatUsername.BankProfileId)
          {
              return new AdminUserStatus { Message = "Phone Number Already Exit", IsDuplicate = true };
          }
        }
        else
        {
          return new AdminUserStatus { Message = "Phone Number Already Exit", IsDuplicate = true};
        }
        
      }
      return new AdminUserStatus { Message = "", IsDuplicate = false};
    }
  
  }
}