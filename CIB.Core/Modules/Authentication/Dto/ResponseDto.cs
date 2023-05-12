using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CIB.Core.Modules.Authentication.Dto
{
    public class ResponseDto
    {
        
    }
    public class UserAccessModel 
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsCorporate { get; set; }
    }

    public class LoginResponsedata
    {
        public string Responsecode { get; set; }
        public string ResponseDescription { get; set; }
        public int UserpasswordChanged { get; set; }
        public string CustomerIdentity { get; set; }
        public Guid UserId { get; set; }
        public string DefaultAccountNumber { get; set; }
        public string DefaultAccountName { get; set; }
        public string DefaultAccountBalance { get; set; }
        public string LastLoginDate { get; set; }
        public string Phone { get; set; }
        public string BVN { get; set; }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string SecurityQuestion { get; set; }
        public string AuthorizationType {get;set;}
        //public List<MyAccounts> AccountList { get; set; }
        //public List<dystementofacct> TransactionHistory { get; set; }
        public string CustomerID { get; set; }
        public int IndemnitySigned { get; set; }
        public string Role { get; set; }
        public string RoleId { get; set; }
        public List<UserAccessModel> Permissions { get; set; }
        public string CompanyName { get; set; }
        public int? RegStage { get; set; }
        public int? Status { get; set; }
        public Guid? CorporateCustomerId { get; set; }
    }
}