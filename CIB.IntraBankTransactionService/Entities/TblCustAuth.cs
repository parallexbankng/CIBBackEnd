using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.IntraBankTransactionService.Entities
{
    public partial class TblCustAuth
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string Username { get; set; }
        public string Phone1 { get; set; }
        public int? IsPhoneVerified { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string TranPin { get; set; }
        public int? IsExistingCustomer { get; set; }
        public string UserRoles { get; set; }
        public Guid? CorporateRole { get; set; }
        public string AccountType { get; set; }
        public string ProductType { get; set; }
        public string CustomerType { get; set; }
        public string Tier { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public int? DoYouHaveBvn { get; set; }
        public string Bvn { get; set; }
        public string Bvnphone { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public decimal? AcctBalance { get; set; }
        public int? IsBvnverified { get; set; }
        public DateTime? LastLogin { get; set; }
        public string GeneratedCode { get; set; }
        public bool? CodeExpired { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateCompleted { get; set; }
        public string CustomerId { get; set; }
        public string Branch { get; set; }
        public int? Passwordchanged { get; set; }
        public int? Status { get; set; }
        public string MaidenName { get; set; }
        public string DeviceId { get; set; }
        public int? NoOfWrongAttempts { get; set; }
        public Guid? JobCriteria { get; set; }
        public int? RegStage { get; set; }
        public int? DoYouHaveNin { get; set; }
        public string Nin { get; set; }
        public Guid? HowHeardAboutLotus { get; set; }
        public string MaritalStatus { get; set; }
        public string IdentityType { get; set; }
        public string Idnumber { get; set; }
        public DateTime? IdissueDate { get; set; }
        public DateTime? IdexpiryDate { get; set; }
        public string IdissuedCountry { get; set; }
        public string IdissuedState { get; set; }
        public string CountryOfResidence { get; set; }
        public string StateOfResidence { get; set; }
        public string CityOfResident { get; set; }
        public string Zipcode { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string CurrentOtp { get; set; }
        public DateTime? CurrentOtptime { get; set; }
        public string ReferenceCode { get; set; }
        public string Occupation { get; set; }
        public string ProductClass { get; set; }
        public int? IbankOnboarded { get; set; }
        public string SecurityQuestion { get; set; }
        public string SecurityAnswer { get; set; }
        public int? FromIbankSigup { get; set; }
        public string ReasonsForDeactivation { get; set; }
        public DateTime? LastLoginAttempt { get; set; }
        public int? FromMobileApp { get; set; }
        public int? Loggon { get; set; }
        public int? Otptrycount { get; set; }
        public DateTime? LastActivity { get; set; }
        public int? ResetInitiated { get; set; }
        public int? IndemnitySigned { get; set; }
        public DateTime? IndemnitySignedDate { get; set; }
        public string CurrentTk { get; set; }
        public string Otpamount { get; set; }
        public string OtpcreditAmount { get; set; }
        public string OtpdebitAccount { get; set; }
        public int? TokenAuthForMobile { get; set; }
        public int? SendIntLoginEmail { get; set; }
        public DateTime? LastIntLogin { get; set; }
        public int? SendMobLoginEmail { get; set; }
        public DateTime? LastMobLogin { get; set; }
    }
}
