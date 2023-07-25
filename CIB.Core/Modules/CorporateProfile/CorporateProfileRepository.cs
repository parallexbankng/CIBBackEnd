using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common.Repository;
using CIB.Core.Entities;
using CIB.Core.Modules.CorporateProfile.Dto;

namespace CIB.Core.Modules.CorporateProfile
{
  public class CorporateProfileRepository : Repository<TblCorporateProfile>, ICorporateProfileRepository
  {
    public CorporateProfileRepository(ParallexCIBContext context) : base(context)
    {
    }
    public ParallexCIBContext context
    {
      get { return _context as ParallexCIBContext; }
    }

    public CorporateUserStatus CheckDuplicate(TblCorporateProfile profile, Guid CorporateCustomerId, bool isUpdate = false)
    {
        var duplicatUsername = _context.TblCorporateProfiles.FirstOrDefault(x => x.Username != null && x.Username.Trim().ToLower().Equals(profile.Username.Trim().ToLower()) && x.CorporateCustomerId == CorporateCustomerId);
        var duplicatePhone = _context.TblCorporateProfiles.FirstOrDefault(x => x.Phone1 != null && x.Phone1.Trim().Equals(profile.Phone1.Trim()) && x.CorporateCustomerId == CorporateCustomerId);
        var duplicateEmail = _context.TblCorporateProfiles.FirstOrDefault(x => x.Email != null && x.Email.Trim().ToLower().Equals(profile.Email.Trim().ToLower()) && x.CorporateCustomerId == CorporateCustomerId);

        if(duplicatUsername != null)
        {
          if(isUpdate)
          {
            if(profile.Id != duplicatUsername.Id)
            {
              return new CorporateUserStatus { Message = "User Name Already Exit", IsDuplicate = "01" };
            }
          }
          else 
          {
            return new CorporateUserStatus { Message = "User Name Already Exit", IsDuplicate = "01" };
          }
        }
        if(duplicateEmail != null)
        {
          if(isUpdate)
          {
            if(profile.Id != duplicateEmail.Id)
            {
              return new CorporateUserStatus { Message = "Email Address Already Exit", IsDuplicate = "01" };
            }
          }
          else
          {
            return new CorporateUserStatus { Message = "Email Address Already Exit", IsDuplicate = "01" };
          }
          
        }
        if(duplicatePhone != null)
        {
          if(isUpdate)
          {
            if(profile.Id != duplicatePhone.Id)
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

    public IEnumerable<CorporateProfileResponseDto> GetAllCorporateProfiles()
    {
       var corporateProfileModel = (
                from corporateProfile in _context.TblCorporateProfiles.OrderBy(m=>m.Sn)
                join role in _context.TblCorporateRoles on corporateProfile.CorporateRole.ToString() equals role.Id.ToString()
                select new CorporateProfileResponseDto
                {
                    Id = corporateProfile.Id,
                    AcctBalance = corporateProfile.AcctBalance,
                    Address1 = corporateProfile.Address1,
                    Address2 = corporateProfile.Address2,
                    ApprovalLimit = corporateProfile.ApprovalLimit,
                    FromMobileApp = corporateProfile.FromMobileApp,
                    LastActivity = corporateProfile.LastActivity,
                    LastLoginAttempt = corporateProfile.LastLoginAttempt,
                    NoOfWrongAttempts = corporateProfile.NoOfWrongAttempts,
                    OtpcreditAmount = corporateProfile.OtpcreditAmount,
                    OtpdebitAccount = corporateProfile.OtpdebitAccount,
                    SecurityQuestion = corporateProfile.SecurityQuestion,
                    CityOfResident = corporateProfile.CityOfResident,
                    CodeExpired = corporateProfile.CodeExpired,
                    CorporateCustomerId = corporateProfile.CorporateCustomerId,
                    CorporateRole = corporateProfile.CorporateRole,
                    CorporateRoleName = role.RoleName,
                    CountryOfResidence = corporateProfile.CountryOfResidence,
                    CustomerType = corporateProfile.CustomerType,
                    DateOfBirth = corporateProfile.DateOfBirth,
                    ReasonsForDeactivation = corporateProfile.ReasonsForDeactivation,
                    ReasonsForDeclining = corporateProfile.ReasonsForDeclining,
                    Email = corporateProfile.Email,
                    FirstName = corporateProfile.FirstName,
                    FullName = corporateProfile.FullName,
                    Gender = corporateProfile.Gender,
                    LastLogin = corporateProfile.LastLogin,
                    LastName = corporateProfile.LastName,
                    MaidenName = corporateProfile.MaidenName,
                    MaritalStatus = corporateProfile.MaritalStatus,
                    MiddleName = corporateProfile.MiddleName,
                    Nationality = corporateProfile.Nationality,
                    Nin = corporateProfile.Nin,
                    Occupation = corporateProfile.Occupation,
                    Phone1 = corporateProfile.Phone1,
                    Phone2 = corporateProfile.Phone2,
                    ProductClass = corporateProfile.ProductClass,
                    Sn = corporateProfile.Sn,
                    Status = corporateProfile.Status,
                    StateOfResidence = corporateProfile.StateOfResidence,
                    ReferenceCode = corporateProfile.ReferenceCode,
                    RegStage = corporateProfile.RegStage,
                    ResetInitiated = corporateProfile.ResetInitiated,
                    Title = corporateProfile.Title,
                    Username = corporateProfile.Username,
                    UserRoleName = role.RoleName,
                    UserRoles = corporateProfile.CorporateRole.ToString(),
                    Zipcode = corporateProfile.Zipcode
                }).ToList();
            return corporateProfileModel.OrderByDescending(ctx => ctx.Sn);
    }

    public  IEnumerable<CorporateProfileResponseDto> GetSingleSignatoryCorporateProfilesByCorporateCustomerId(Guid id)
    {
      var corporateProfileModel = (
                from corporateProfile in _context.TblCorporateProfiles.Where(m=>m.CorporateCustomerId == id)
                select new CorporateProfileResponseDto
                {
                    Id = corporateProfile.Id,
                    AcctBalance = corporateProfile.AcctBalance,
                    Address1 = corporateProfile.Address1,
                    Address2 = corporateProfile.Address2,
                    ApprovalLimit = corporateProfile.ApprovalLimit,
                    FromMobileApp = corporateProfile.FromMobileApp,
                    LastActivity = corporateProfile.LastActivity,
                    LastLoginAttempt = corporateProfile.LastLoginAttempt,
                    NoOfWrongAttempts = corporateProfile.NoOfWrongAttempts,
                    OtpcreditAmount = corporateProfile.OtpcreditAmount,
                    OtpdebitAccount = corporateProfile.OtpdebitAccount,
                    SecurityQuestion = corporateProfile.SecurityQuestion,
                    CityOfResident = corporateProfile.CityOfResident,
                    CodeExpired = corporateProfile.CodeExpired,
                    CorporateCustomerId = corporateProfile.CorporateCustomerId,
                    CorporateRole = corporateProfile.CorporateRole,
                    CorporateRoleName = "",
                    CountryOfResidence = corporateProfile.CountryOfResidence,
                    CustomerType = corporateProfile.CustomerType,
                    DateOfBirth = corporateProfile.DateOfBirth,
                    ReasonsForDeactivation = corporateProfile.ReasonsForDeactivation,
                    ReasonsForDeclining = corporateProfile.ReasonsForDeclining,
                    Email = corporateProfile.Email,
                    FirstName = corporateProfile.FirstName,
                    FullName = corporateProfile.FullName,
                    Gender = corporateProfile.Gender,
                    LastLogin = corporateProfile.LastLogin,
                    LastName = corporateProfile.LastName,
                    MaidenName = corporateProfile.MaidenName,
                    MaritalStatus = corporateProfile.MaritalStatus,
                    MiddleName = corporateProfile.MiddleName,
                    Nationality = corporateProfile.Nationality,
                    Nin = corporateProfile.Nin,
                    Occupation = corporateProfile.Occupation,
                    Phone1 = corporateProfile.Phone1,
                    Phone2 = corporateProfile.Phone2,
                    ProductClass = corporateProfile.ProductClass,
                    Sn = corporateProfile.Sn,
                    Status = corporateProfile.Status,
                    StateOfResidence = corporateProfile.StateOfResidence,
                    ReferenceCode = corporateProfile.ReferenceCode,
                    RegStage = corporateProfile.RegStage,
                    ResetInitiated = corporateProfile.ResetInitiated,
                    Title = corporateProfile.Title,
                    Username = corporateProfile.Username,
                    UserRoleName = "",
                    UserRoles = corporateProfile.CorporateRole.ToString(),
                    Zipcode = corporateProfile.Zipcode
                }).ToList();
            return corporateProfileModel.OrderByDescending(ctx => ctx.Sn);
    }
    
    public IEnumerable<CorporateProfileResponseDto> GetAllCorporateProfilesByCorporateCustomerId(Guid CorporateCustomerId)
    {
      var corporateProfileModel = (
                from corporateProfile in _context.TblCorporateProfiles.Where(a => a.CorporateCustomerId != null && a.CorporateCustomerId == CorporateCustomerId)
                join role in _context.TblCorporateRoles on corporateProfile.CorporateRole.ToString() equals role.Id.ToString()
                select new CorporateProfileResponseDto
                {
                    Id = corporateProfile.Id,
                    AcctBalance = corporateProfile.AcctBalance,
                    Address1 = corporateProfile.Address1,
                    Address2 = corporateProfile.Address2,
                    ApprovalLimit = corporateProfile.ApprovalLimit,
                    FromMobileApp = corporateProfile.FromMobileApp,
                    LastActivity = corporateProfile.LastActivity,
                    LastLoginAttempt = corporateProfile.LastLoginAttempt,
                    NoOfWrongAttempts = corporateProfile.NoOfWrongAttempts,
                    OtpcreditAmount = corporateProfile.OtpcreditAmount,
                    OtpdebitAccount = corporateProfile.OtpdebitAccount,
                    SecurityQuestion = corporateProfile.SecurityQuestion,
                    CityOfResident = corporateProfile.CityOfResident,
                    CodeExpired = corporateProfile.CodeExpired,
                    CorporateCustomerId = corporateProfile.CorporateCustomerId,
                    CorporateRole = corporateProfile.CorporateRole,
                    CorporateRoleName = role.RoleName,
                    CountryOfResidence = corporateProfile.CountryOfResidence,
                    CustomerType = corporateProfile.CustomerType,
                    DateOfBirth = corporateProfile.DateOfBirth,
                    ReasonsForDeactivation = corporateProfile.ReasonsForDeactivation,
                    ReasonsForDeclining = corporateProfile.ReasonsForDeclining,
                    Email = corporateProfile.Email,
                    FirstName = corporateProfile.FirstName,
                    FullName = corporateProfile.FullName,
                    Gender = corporateProfile.Gender,
                    LastLogin = corporateProfile.LastLogin,
                    LastName = corporateProfile.LastName,
                    MaidenName = corporateProfile.MaidenName,
                    MaritalStatus = corporateProfile.MaritalStatus,
                    MiddleName = corporateProfile.MiddleName,
                    Nationality = corporateProfile.Nationality,
                    Nin = corporateProfile.Nin,
                    Occupation = corporateProfile.Occupation,
                    Phone1 = corporateProfile.Phone1,
                    Phone2 = corporateProfile.Phone2,
                    ProductClass = corporateProfile.ProductClass,
                    Sn = corporateProfile.Sn,
                    Status = corporateProfile.Status,
                    StateOfResidence = corporateProfile.StateOfResidence,
                    ReferenceCode = corporateProfile.ReferenceCode,
                    RegStage = corporateProfile.RegStage,
                    ResetInitiated = corporateProfile.ResetInitiated,
                    Title = corporateProfile.Title,
                    Username = corporateProfile.Username,
                    UserRoleName = role.RoleName,
                    UserRoles = corporateProfile.CorporateRole.ToString(),
                    Zipcode = corporateProfile.Zipcode
                }).ToList();
            return corporateProfileModel.OrderByDescending(ctx => ctx.Sn);
    }

    public TblCorporateProfile GetCorporateCustomerIdByUserName(string userName)
    {
      return _context.TblCorporateProfiles.SingleOrDefault(a => a.Username != null && a.Username.ToLower() == userName.ToLower());
    }

    public TblCorporateProfile GetProfileByEmail(string email)
    {
      return _context.TblCorporateProfiles.SingleOrDefault(a => a.Email != null && a.Email.ToLower() == email.ToLower());
    }

    public TblCorporateProfile GetProfileByID(Guid id)
    {
      return _context.TblCorporateProfiles.SingleOrDefault(a => a.Id == id);
    }
    public TblCorporateProfile GetProfileByPhoneNumber(string phoneNumber)
    {
      throw new NotImplementedException();
    }
    public TblCorporateProfile GetProfileByUserName(string username)
    {
      return _context.TblCorporateProfiles.Where(a => a.Username.Equals(username)).FirstOrDefault();
    }
    public string GetProfileRoleName(string userName)
    {
      //  var result = _context.TblCorporateProfiles.Where(x => x.Username != null && x.Username.ToLower() == userName.ToLower())
      //               .Join(_context.TblRoles, cp => cp.CorporateRole, r => r.Id.ToString(), (cp, r) => new { RoleName = r.RoleName })
      //               .SingleOrDefault();
      //           return result?.RoleName;
      return "";
    }
    public CorporateProfileResponseDto RetrieveProfileByID(Guid id)
    {
      var corporateProfileModel = (
                from corporateProfile in _context.TblCorporateProfiles.Where(a => a.Id == id)
                from role in _context.TblRoles.Where(x => x.Id.ToString().Equals(corporateProfile.CorporateRole)).DefaultIfEmpty()
                from corprole in _context.TblCorporateRoles.Where(x => x.Id.Equals(corporateProfile.CorporateRole.Value)).DefaultIfEmpty()
                select new CorporateProfileResponseDto
                {
                    Id = corporateProfile.Id,
                    AcctBalance = corporateProfile.AcctBalance,
                    Address1 = corporateProfile.Address1,
                    Address2 = corporateProfile.Address2,
                    ApprovalLimit = corporateProfile.ApprovalLimit,
                    FromMobileApp = corporateProfile.FromMobileApp,
                    LastActivity = corporateProfile.LastActivity,
                    LastLoginAttempt = corporateProfile.LastLoginAttempt,
                    NoOfWrongAttempts = corporateProfile.NoOfWrongAttempts,
                    OtpcreditAmount = corporateProfile.OtpcreditAmount,
                    OtpdebitAccount = corporateProfile.OtpdebitAccount,
                    SecurityQuestion = corporateProfile.SecurityQuestion,
                    Branch = corporateProfile.Branch,
                    CityOfResident = corporateProfile.CityOfResident,
                    CodeExpired = corporateProfile.CodeExpired,
                    CorporateCustomerId = corporateProfile.CorporateCustomerId,
                    CorporateRole = corporateProfile.CorporateRole,
                    CorporateRoleName = corprole.RoleName,
                    CountryOfResidence = corporateProfile.CountryOfResidence,
                    CustomerType = corporateProfile.CustomerType,
                    DateOfBirth = corporateProfile.DateOfBirth,
                    ReasonsForDeactivation = corporateProfile.ReasonsForDeactivation,
                    ReasonsForDeclining = corporateProfile.ReasonsForDeclining,
                    Email = corporateProfile.Email,
                    FirstName = corporateProfile.FirstName,
                    FullName = corporateProfile.FullName,
                    Gender = corporateProfile.Gender,
                    LastLogin = corporateProfile.LastLogin,
                    LastName = corporateProfile.LastName,
                    MaidenName = corporateProfile.MaidenName,
                    MaritalStatus = corporateProfile.MaritalStatus,
                    MiddleName = corporateProfile.MiddleName,
                    Nationality = corporateProfile.Nationality,
                    Nin = corporateProfile.Nin,
                    Occupation = corporateProfile.Occupation,
                    Phone1 = corporateProfile.Phone1,
                    Phone2 = corporateProfile.Phone2,
                    ProductClass = corporateProfile.ProductClass,
                    Sn = corporateProfile.Sn,
                    Status = corporateProfile.Status,
                    StateOfResidence = corporateProfile.StateOfResidence,
                    ReferenceCode = corporateProfile.ReferenceCode,
                    RegStage = corporateProfile.RegStage,
                    ResetInitiated = corporateProfile.ResetInitiated,
                    Title = corporateProfile.Title,
                    Username = corporateProfile.Username,
                    UserRoleName = role.RoleName,
                    UserRoles = corprole.RoleName,
                    Zipcode = corporateProfile.Zipcode
                })?.SingleOrDefault();
            return corporateProfileModel;
    }
    public void UpdateCorporateProfile(TblCorporateProfile update)
    {
      _context.Update(update).Property(x=>x.Sn).IsModified = false;
    }
    public TblCorporateProfile GetCorporateProfileByEmail(string email)
    {
      return _context.TblCorporateProfiles.Where(a => a.Email.Equals(email)).FirstOrDefault();
    }
    public TblCorporateProfile GetProfileByUserNameAndCustomerId(string userName, Guid CorporateCustomerId)
    {
      return _context.TblCorporateProfiles.Where(a => a.CorporateCustomerId == CorporateCustomerId && a.Username.Equals(userName)).FirstOrDefault();
    }
    public TblCorporateProfile GetProfileByEmailAndCustomerId(string email, Guid CorporateCustomerId)
    {
      return _context.TblCorporateProfiles.Where(a => a.CorporateCustomerId == CorporateCustomerId && a.Email != null && a.Email.Trim().ToLower().Equals(email.Trim().ToLower())).FirstOrDefault();
    }
    public IEnumerable<TblCorporateProfile> GetCorporateProfilesByRole(string role)
    {
      var systemRole = _context.TblCorporateRoles.FirstOrDefault(ctx => ctx.RoleName.Trim().ToLower() == role);
      var corporateProfileModel = _context.TblCorporateProfiles.Where(ctx => ctx.CorporateRole == systemRole.Id).ToList();
      return corporateProfileModel;
    }

    public TblCorporateProfile GetProfileByUserIdAndCustomerId(Guid userId, Guid CorporateCustomerId)
    {
      return _context.TblCorporateProfiles.FirstOrDefault(a => a.Id == userId && a.CorporateCustomerId == CorporateCustomerId);
    }
    public bool IsAdminActive(Guid roleId, Guid CorporateCustomerId)
    {
      var systemRole = _context.TblCorporateRoles.FirstOrDefault(ctx => ctx.Id == roleId);
      if(systemRole.RoleName.Trim().ToLower() != "corporate admin")
      {
        return false;
      }
      var corporateProfileModel = _context.TblCorporateProfiles.Where(ctx => ctx.CorporateCustomerId == CorporateCustomerId && ctx.CorporateRole == roleId && ctx.Status == 1).ToList();
      if(corporateProfileModel.Count != 0)
      {
        return true;
      }
      return false;
    }
  }
}