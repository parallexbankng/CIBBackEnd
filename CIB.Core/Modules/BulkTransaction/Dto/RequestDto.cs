using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIB.Core.Modules.Transaction.Dto;
using Microsoft.AspNetCore.Http;

namespace CIB.Core.Modules.BulkTransaction.Dto
{
    public class VerifyBulkTransactionDto
    {
        public string SourceAccountNumber { get; set; }
        public string Narration { get; set; }
        //public string Amount { get; set; }
        //public string TransactionType { get; set; }
        public string WorkflowId {get;set;}
        public string Currency {get;set;}
        public IFormFile files { get; set; }
    }

    public class VerifyBulkTransaction
    {
        public string SourceAccountNumber { get; set; }
        public string Narration { get; set; }
        //public string TransactionType { get; set; }
        public Guid? WorkflowId {get;set;}
        public string Currency {get;set;}
        public IFormFile files { get; set; }
    }

    public class InitiateBulkTransactionDto : BaseTransactioDto
    {
        public string SourceAccountNumber { get; set; }
        public string Narration { get; set; }
        public string WorkflowId {get;set;}
        public string Currency {get;set;}
        public IFormFile files { get; set; }
        public string Otp { get; set; }
        public string DebitMode { get; set; }
        public string TransactionLocation { get; set; }
        public string AllowDuplicateAccount {get;set;}
    }
     public class InitiateBulkTransaction : BaseTransactioDto
    {
        public string SourceAccountNumber { get; set; }
        public string Narration { get; set; }
        public Guid? WorkflowId {get;set;}
        public string Currency {get;set;}
        public IFormFile files { get; set; }
        public string Otp { get; set; }
        public string TransactionLocation { get; set; }
        public bool AllowDuplicateAccount {get;set;}
    }
}