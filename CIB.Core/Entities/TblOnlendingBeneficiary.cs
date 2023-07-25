using System;
using System.Collections.Generic;

#nullable disable

namespace CIB.Core.Entities
{
    public partial class TblOnlendingBeneficiary
    {
        public Guid Id { get; set; }
        public long Sn { get; set; }
        public Guid? CorporateCustomerId { get; set; }
        public string SurName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string PhoneNo { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Bvn { get; set; }
        public string IdNumber { get; set; }
        public DateTime? IdIssuedDate { get; set; }
        public decimal? FundAmount { get; set; }
        public int? Status { get; set; }
        public Guid? BatchId { get; set; }
        public string BvnResponse { get; set; }
        public string BvnResponseCode { get; set; }
        public string IdNumberResponse { get; set; }
        public string IdNumberResponseCode { get; set; }
        public int? VerificationStatus { get; set; }
        public DateTime? DateCreated { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string DocType { get; set; }
        public string StreetNo { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public string StateOfResidence { get; set; }
        public string PlaceOfBirth { get; set; }
        public string MaritalStatus { get; set; }
        public string Region { get; set; }
        public string Nationality { get; set; }
        public string Title { get; set; }
    }
}
