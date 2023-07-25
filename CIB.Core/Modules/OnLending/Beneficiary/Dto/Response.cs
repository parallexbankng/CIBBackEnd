using System;
using System.Collections.Generic;

namespace CIB.Core.Modules.OnLending.Beneficiary.Dto
{
    public class Response
    {
        
    }
    public class BeneficiaryDto
    {
        public Guid? Id {get;set;}
        public string? Title { get; set; }
        public string? SurName{get;set;}
        public string? FirstName	 {get;set;}
        public string? MiddleName {get;set;}
        public string? PhoneNo {get;set;}
        public string? Email {get;set;}
        public string? Gender {get;set;}	
        public string? StreetNo {get;set;}	
        public string? Address {get;set;}	
        public string? City {get;set;}	
        public string? State {get;set;}	
        public string? Lga {get;set;}	
        public string? DateOfBirth {get;set;}
        public string? Bvn {get;set;}
        public string? AccountNumber {get;set;}
        public string? DocType {get;set;}
        public string? IdNumber {get;set;}
        public string? DateIssued {get;set;}
        public string? StateOfResidence {get;set;}
        public string? PlaceOfBirth {get;set;}
        public string? MaritalStatus {get;set;}
        public string? Region {get;set;}
        public decimal? FundAmount {get;set;}
        public string?  PreferredNarration {get;set;}
        public string? RepaymentDate {get;set;}
        public string? Nationality {get;set;}
        public string?  Error {get;set;}
    }
    public class VerifyResponse
    {
        public decimal? TotalAmount {get;set;}
        public int TotalNumber {get;set;}
        public List<BeneficiaryDto> Beneficiaries {get;set;}
    }

    
}