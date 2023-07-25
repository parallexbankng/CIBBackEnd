using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Common;

namespace CIB.Core.Modules.Authentication.Dto
{
    public class RequestDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class ResetPasswordModel : BaseDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Code { get; set; }
        public string CustomerId {get;set;}
    }
    public class CorporateFirstLoginPasswordChangeModel:BaseDto
    {
        /// <summary>
        /// User Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// CustomerID
        /// </summary>
        public string CustomerID { get; set; }

        /// <summary>
        /// User current password.
        /// </summary>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// User new password
        /// </summary>
        public string NewPassword { get; set; }
    }
    public class FirstLoginPasswordChangeModel:BaseDto
    {
        /// <summary>
        /// User Name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// User current password.
        /// </summary>
        public string CurrentPassword { get; set; }

        /// <summary>
        /// User new password
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// Customer Id
        /// </summary>
        public string CustomerId { get; set; }
    }

    public class BankUserLoginParam: BaseDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }
    public class UserData
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }

    public class ChangeUserPasswordParam: BaseDto
    {
        public string Id {get;set;}
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ComfirmPassword { get; set; }
    }
    public class CustomerLoginParam : BaseDto
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public string CustomerID { get; set; }
        public string OTP { get; set; }
    }

    public class CorporateUserModel
    {
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string? Phone1 { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? CustomerID { get; set; }
        public string? CorporateCustomerId { get; set; }
    }

}