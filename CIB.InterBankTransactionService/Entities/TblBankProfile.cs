using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.InterBankTransactionService.Entities
{
    public partial class TblBankProfile
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public string Username { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string UserRoles { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? LastLogin { get; set; }
        public string GeneratedCode { get; set; }
        public bool? CodeExpired { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateCompleted { get; set; }
        public string Branch { get; set; }
        public int? Passwordchanged { get; set; }
        public int? Status { get; set; }
        public int? NoOfWrongAttempts { get; set; }
        public int? RegStage { get; set; }
        public string MaritalStatus { get; set; }
        public string SecurityQuestion { get; set; }
        public string SecurityAnswer { get; set; }
        public string ReasonsForDeactivation { get; set; }
        public DateTime? LastLoginAttempt { get; set; }
        public int? Loggon { get; set; }
        public DateTime? LastActivity { get; set; }
        public int? ResetInitiated { get; set; }
        public int? SendLoginEmail { get; set; }
        public string ReasonsForDeclining { get; set; }
        public string SecurityQuestion2 { get; set; }
        public string SecurityAnswer2 { get; set; }
        public string SecurityQuestion3 { get; set; }
        public string SecurityAnswer3 { get; set; }
        public int? SecurityStage { get; set; }
        public DateTime? PasswordExpiryDate { get; set; }
        public int? IsDefault { get; set; }
    }
}
