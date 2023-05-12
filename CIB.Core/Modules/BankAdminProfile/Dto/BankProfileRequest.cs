using System;
using CIB.Core.Common;

namespace CIB.Core.Modules.BankAdminProfile.Dto
{
    public class CreateBankAdminProfileDTO : BaseDto
    {
        
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string UserRoleId { get; set; }
        public string Password { get; set; }
    }
    public class UpdateBankAdminProfileDTO : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string UserRoleId { get; set; }
        public string Password { get; set; }
    }

    public class UpdateBankAdminProfile : BaseUpdateDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string UserRoleId { get; set; }
        public string Password { get; set; }
    }
    public class DeclineBankAdminProfileDTO : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public string Reason { get; set; }
    }
    public class DeactivateBankAdminProfileDTO : DeclineBankAdminProfileDTO
    {
    }
    public class UpdateBankAdminProfileUserRoleDTO : BaseUpdateDto
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
    }

    public class UpdateBankAdminProfileUserRole :BaseUpdateDto
    {
        public string Id { get; set; }
        public string RoleId { get; set; }
    }
    public class AdminUserStatus {
        public string Message { get; set; }
        public bool IsDuplicate { get; set; }
    }

    public class ChangePasswordDto : BaseUpdateDto
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

    public class ResetPasswordDto : BaseUpdateDto
    {
        public Guid Id { get; set; }
    }
     public class ResetPassword : BaseUpdateDto
    {
        public string Id { get; set; }
    }

    public class ForgetPassword : BaseUpdateDto
    {
        public string Email { get; set; }
        public string CustomerId {get;set;}
    }

    // public class BankProfileNotificatinDto
    // {
    //     public string Email { get; set; }
    //     public string Email { get; set; }
    //     public string Email { get; set; }
    // }
}