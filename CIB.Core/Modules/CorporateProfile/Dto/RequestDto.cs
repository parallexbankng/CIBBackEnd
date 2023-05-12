using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.CorporateProfile.Dto
{
  public class CreateProfile:BaseDto
  {
    public string CorporateCustomerId { get; set; }
    public string CorporateRoleId { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string ApprovalLimit { get; set; }
    public string LastName { get; set; }
    public string Password { get; set; }
  }

  public class CreateProfileDto:BaseDto
  {
    public Guid? CorporateCustomerId { get; set; }
    public Guid? CorporateRoleId { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public string LastName { get; set; }
    public string Password { get; set; }
  }

  public class UpdateProfile:BaseUpdateDto
  {
    public string Id { get; set; }
    public string CorporateCustomerId { get; set; }
    public string CorporateRoleId { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string ApprovalLimit { get; set; }
    public string LastName { get; set; }
    public string Action { get; set; }
    public string Password { get; set; }
  }

  public class UpdateProfileDTO : BaseUpdateDto
  {
    public Guid Id { get; set; }
    public Guid? CorporateCustomerId { get; set; }
    public Guid? CorporateRoleId { get; set; }
    public string Username { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public string LastName { get; set; }
    public string Password { get; set; }
    public string Action { get; set; }
  }


  public class DeclineProfileDTO: BaseUpdateDto
  {
    public Guid Id { get; set; }
    public string Reason { get; set; }
  }
  public class DeclineProfile: BaseUpdateDto
  {
    public string Id { get; set; }
    public string Reason { get; set; }
  }
  public class DeactivateProfileDTO : DeclineProfileDTO
  {

  }

  public class UpdateProfileUserRoleDTO :BaseUpdateDto
  {
    public Guid Id { get; set; }
    public string RoleId { get; set; }
  }
  public class UpdateCorporateUserRoleDTO :BaseUpdateDto
  {
    public string Id { get; set; }
    public string RoleId { get; set; }
  }
  public class CorporateUserStatus 
  {
    public string Message { get; set; }
    public string IsDuplicate { get; set; }
  }

  public class DuplicateStatus 
  {
    public string Message { get; set; }
    public bool IsDuplicate { get; set; }
  }

   public class CorporateChangePasswordDto : BaseUpdateDto
    {
        /// <summary>
        /// User current password.
        /// </summary>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// User new password
        /// </summary>
        public string NewPassword { get; set; }
    }

    public class CorporateResetPasswordDto
    {
        public Guid Id { get; set; }
        public string IPAddress { get; set; }
        public string ClientStaffIPAddress { get; set; }
        public string MACAddress { get; set; }
        public string HostName { get; set; }
    }

    public class CorporateResetPassword
    {
        public string Id { get; set; }
        public string IPAddress { get; set; }
        public string ClientStaffIPAddress { get; set; }
        public string MACAddress { get; set; }
        public string HostName { get; set; }
    }
     public class CustomerNameEnquiryModel
    {
        public string Username { get; set; }
        public string CustomerID { get; set; }
    }

    public class CustomerNameEnquiryResponseModel
    {
        public string responseCode { get; set; }
        public string responseDescription { get; set; }
        public CustomerNameEnquiryResponseDataModel data { get; set; }
    }

    public class CustomerNameEnquiryResponseDataModel
    {
        public string customerFirstName { get; set; }
        public string customerMiddleName { get; set; }
        public string customerLastName { get; set; }
        public string customerEmail { get; set; }
        public string customerPhoneNo { get; set; }
    }
}