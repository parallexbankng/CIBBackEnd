using System;
using System.Collections.Generic;
using System.Linq;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.BankAdminProfile.Dto;

namespace CIB.Core.Modules.BankAdminProfile
{
  public class BankProfileRepository : Repository<TblBankProfile>, IBankProfileRepository
  {
    public BankProfileRepository(ParallexCIBContext context) : base(context)
    {

    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }
    public AdminUserStatus CheckDuplicate(TblBankProfile update, Guid? profileId = null)
    {
      var duplicatePhone = _context.TblBankProfiles.Where(x => x.Phone == update.Phone).Any();
      var duplicateEmail = _context.TblBankProfiles.Where(x => x.Email.ToLower().Trim() == update.Email.ToLower().Trim()).Any();
      var duplicateUserName = _context.TblBankProfiles.Where(x => x.Username.Equals(update.Username)).Any();
      
      if(duplicateUserName)
      {
        if(profileId.HasValue)
        {
          if(update.Id != profileId.Value)
          {
            return new AdminUserStatus { Message = "User Name Already Exit", IsDuplicate = true };
          }
        }
        else
        {
          return new AdminUserStatus { Message = "User Name Already Exit", IsDuplicate = true };
        }
      }
      if(duplicateEmail)
      {
        if(profileId.HasValue)
        {
          if(update.Id != profileId.Value)
          {
            return new AdminUserStatus { Message = "Email Address Already Exit", IsDuplicate = true };
          }
        }
        else
        {
          return new AdminUserStatus { Message = "Email Address Already Exit", IsDuplicate = true };
        }
      }
      if(duplicatePhone)
      {
        if(profileId.HasValue)
        {
          if(update.Id != profileId.Value)
          {
            return new AdminUserStatus { Message = "Phone Number Already Exit", IsDuplicate = true };
          }
        }
        else
        {
          return new AdminUserStatus { Message = "Phone Number Already Exit", IsDuplicate = true };
        }
      }  
      return new AdminUserStatus { Message = "ok", IsDuplicate = false };
    }
   
    public AdminUserStatus CheckDuplicates(TblBankProfile profile, bool isUpdate = false)
    {
        var duplicatUsername = _context.TblBankProfiles.FirstOrDefault(x => x.Username != null && x.Username.Trim().ToLower().Equals(profile.Username.Trim().ToLower()));
        var duplicatePhone = _context.TblBankProfiles.FirstOrDefault(x => x.Phone != null && x.Phone.Trim().Equals(profile.Phone.Trim()));
        var duplicateEmail = _context.TblBankProfiles.FirstOrDefault(x => x.Email != null && x.Email.Trim().ToLower().Equals(profile.Email.Trim().ToLower()));

        if(duplicatUsername != null)
        {
          if(isUpdate)
          {
            if(profile.Id != duplicatUsername.Id)
            {
              return new AdminUserStatus { Message = "User Name Already Exit", IsDuplicate = true};
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
            if(profile.Id != duplicateEmail.Id)
            {
              return new AdminUserStatus { Message = "Email Address Already Exit", IsDuplicate = true };
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
            if(profile.Id != duplicatePhone.Id)
            {
                return new AdminUserStatus { Message = "Phone Number Already Exit", IsDuplicate = true};
            }
          }
          else
          {
            return new AdminUserStatus { Message = "Phone Number Already Exit", IsDuplicate = true};
          }
          
        }
        return new AdminUserStatus { Message = "", IsDuplicate = false};
    }
   
    public IEnumerable<BankAdminProfileResponse> GetAllBankAdminProfiles()
    {
        var bankProfileModel = (from bankProfile in _context.TblBankProfiles.OrderBy(m => m.Sn)
        join role in _context.TblRoles on bankProfile.UserRoles equals role.Id.ToString()
        select new BankAdminProfileResponse
        {
          Id = bankProfile.Id,
          LastActivity = bankProfile.LastActivity,
          LastLoginAttempt = bankProfile.LastLoginAttempt,
          NoOfWrongAttempts = bankProfile.NoOfWrongAttempts,
          SecurityQuestion = bankProfile.SecurityQuestion,
          CodeExpired = bankProfile.CodeExpired,
          DateOfBirth = bankProfile.DateOfBirth,
          ReasonsForDeactivation = bankProfile.ReasonsForDeactivation,
          ReasonsForDeclining = bankProfile.ReasonsForDeclining,
          Email = bankProfile.Email,
          FirstName = bankProfile.FirstName,
          FullName = bankProfile.FullName,
          Gender = bankProfile.Gender,
          PhoneNumber = bankProfile.Phone,
          LastLogin = bankProfile.LastLogin,
          LastName = bankProfile.LastName,
          MaritalStatus = bankProfile.MaritalStatus,
          MiddleName = bankProfile.MiddleName,
          Nationality = bankProfile.Nationality,
          Sn = bankProfile.Sn,
          Status = bankProfile.Status,
          RegStage = bankProfile.RegStage,
          ResetInitiated = bankProfile.ResetInitiated,
          Username = bankProfile.Username,
          UserRoleName = role.RoleName,
          UserRoles = bankProfile.UserRoles
        }).ToList();

      return bankProfileModel.OrderByDescending(ctx => ctx.Sn);
    }
    public BankAdminProfileResponse GetBankAdminProfileById(Guid id)
    {
      var bankProfileModel = (
                from bankProfile in _context.TblBankProfiles.Where(x => x.Status == 1 && x.Id == id)
                from role in _context.TblRoles.Where(x => x.Id.ToString().Equals(bankProfile.UserRoles)).DefaultIfEmpty()
                select new BankAdminProfileResponse
                {
                    Id = bankProfile.Id,
                    LastActivity = bankProfile.LastActivity,
                    LastLoginAttempt = bankProfile.LastLoginAttempt,
                    NoOfWrongAttempts = bankProfile.NoOfWrongAttempts,
                    SecurityQuestion = bankProfile.SecurityQuestion,
                    Branch = bankProfile.Branch,
                    CodeExpired = bankProfile.CodeExpired,
                    DateOfBirth = bankProfile.DateOfBirth,
                    ReasonsForDeactivation = bankProfile.ReasonsForDeactivation,
                    ReasonsForDeclining = bankProfile.ReasonsForDeclining,
                    Email = bankProfile.Email,
                    FirstName = bankProfile.FirstName,
                    FullName = bankProfile.FullName,
                    Gender = bankProfile.Gender,
                    LastLogin = bankProfile.LastLogin,
                    LastName = bankProfile.LastName,
                    MaritalStatus = bankProfile.MaritalStatus,
                    MiddleName = bankProfile.MiddleName,
                    Nationality = bankProfile.Nationality,
                    Sn = bankProfile.Sn,
                    Status = bankProfile.Status,
                    RegStage = bankProfile.RegStage,
                    ResetInitiated = bankProfile.ResetInitiated,
                    Username = bankProfile.Username,
                    UserRoleName = role.RoleName,
                    UserRoles = bankProfile.UserRoles
                })?.FirstOrDefault();
            return bankProfileModel;
    }
    public void UpdateBankProfile(TblBankProfile update)
    {
      _context.Update(update).Property(x=>x.Sn).IsModified = false;
    }
    public TblBankProfile GetProfileByUserName(string userName)
    {
      return _context.TblBankProfiles.Where(ext => ext.Username.Equals(userName)).FirstOrDefault();
    }
    public TblBankProfile GetProfileByEmail(string email)
    {
      return _context.TblBankProfiles.Where(ext => ext.Email.Equals(email)).FirstOrDefault();
    }
    public IEnumerable<TblBankProfile> GetAllBankAdminProfilesByRole(string role)
    {
      var systemRole = _context.TblRoles.FirstOrDefault(ctx => ctx.RoleName.Trim().ToLower() == role);
      var bankProfileModel = _context.TblBankProfiles.Where(ctx => ctx.UserRoles.Trim() == systemRole.Id.ToString()).ToList();
      return bankProfileModel;
    }
  }
}